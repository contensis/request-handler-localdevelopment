using System.Web;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Services;

public class RouteInfoFactory(
    IRequestContext requestContext,
    AppConfiguration appConfiguration,
    DateTime? siteViewQueryCutoffDate = null)
    : IRouteInfoFactory
{
    private readonly DateTime _defaultSiteViewQueryCutoffDate = new(2025, 11, 3, 9, 0, 0);

    public RouteInfo Create(
        Uri baseUri,
        Uri? originUri,
        Headers headers,
        NodeInfo? nodeInfo = null,
        BlockVersionInfo? blockVersionInfo = null,
        string? endpointId = null,
        Guid? layoutRendererId = null,
        ProxyInfo? proxyInfo = null)
    {
        var path = baseUri.AbsolutePath;
        var enableFullUriRouting = blockVersionInfo?.EnableFullUriRouting ?? false;

        var nodeInfoId = nodeInfo?.Id;
        var nodeInfoEntryId = nodeInfo?.EntryId;

        if (blockVersionInfo?.Pushed > (siteViewQueryCutoffDate ?? _defaultSiteViewQueryCutoffDate))
        {
            nodeInfoId = null;
            nodeInfoEntryId = null;
        }

        var queryString = BuildQueryString(originUri, nodeInfoId, nodeInfoEntryId);

        if (blockVersionInfo is not null)
        {
            ApplyBlockClusterRouteDetails(ref baseUri, headers);

            if (!enableFullUriRouting && originUri != null)
            {
                queryString.Add("originPath", originUri.AbsolutePath);
            }
        }

        if (enableFullUriRouting && originUri != null)
        {
            path = originUri.AbsolutePath;
        }

        if (proxyInfo != null)
        {
            path = originUri != null ? originUri.AbsolutePath : path;
        }

        var routeType = blockVersionInfo != null
            ? RouteType.Block
            : proxyInfo != null
                ? RouteType.Proxy
                : RouteType.Url;

        var uri = BuildUri(baseUri, path, queryString);

        return new RouteInfo(
            routeType,
            uri,
            headers,
            nodeInfo?.Path ?? "",
            blockVersionInfo,
            endpointId,
            layoutRendererId,
            proxyInfo)
        {
            DebugData =
            {
                AppConfiguration = appConfiguration,
                NodeInfo = nodeInfo
            }
        };
    }

    public RouteInfo CreateForNonNodePath(
        Uri originUri,
        Headers headers,
        BlockVersionInfo? blockVersionInfo = null)
    {
        var queryString = BuildQueryString(originUri, null, null);

        // Handle API requests
        var path = originUri.AbsolutePath;
        if (originUri.IsContensisApiRequest() &&
            appConfiguration.AliasesWithApiRoutes?.ContainsCaseInsensitive(requestContext.Alias) != true)
        {
            var apiHost = $"api-{requestContext.Alias}.cloud.contensis.com";
            var apiUrl = $"https://{apiHost}";
            var apiUri = new Uri(apiUrl);
            var uri = BuildUri(apiUri, path, queryString);
            headers[Constants.Headers.Host] = apiHost;
            return new RouteInfo(RouteType.Url, uri, headers, "")
            {
                DebugData =
                {
                    AppConfiguration = appConfiguration
                }
            };
        }

        // Handle static paths
        var staticPath = StaticPath.Parse(path);

        if (staticPath is { IsRewritten: true } && blockVersionInfo != null)
        {
            var baseUri = blockVersionInfo.BaseUri;
            ApplyBlockClusterRouteDetails(ref baseUri, headers);
            var uri = BuildUri(baseUri, staticPath.OriginalPath, queryString);

            return new RouteInfo(
                RouteType.Block,
                uri,
                headers,
                "",
                blockVersionInfo)
            {
                DebugData =
                {
                    AppConfiguration = appConfiguration
                }
            };
        }

        return CreateNotFoundRoute(headers);
    }

    public RouteInfo CreateNotFoundRoute(Headers headers, string nodePath = "")
    {
        return new RouteInfo(
            RouteType.NotFound,
            null,
            headers,
            nodePath)
        {
            DebugData =
            {
                AppConfiguration = appConfiguration
            }
        };
    }

    public RouteInfo CreateForIisFallback(Uri originUri, Headers headers, RouteInfo? originalRouteInfo)
    {
        var baseUri = new Uri($"https://{requestContext.LoadBalancerVip}");
        var uri = BuildUri(baseUri, originUri.AbsolutePath, new QueryString(originUri.Query));
        headers[Constants.Headers.Host] = requestContext.IisHostname;
        headers[Constants.Headers.IsIisFallback] = "true";

        return new RouteInfo(RouteType.IisFallback, uri, headers, "")
        {
            DebugData =
            {
                AppConfiguration = appConfiguration,
                InitialDebugData = originalRouteInfo?.DebugData
            }
        };
    }

    private void ApplyBlockClusterRouteDetails(ref Uri baseUri, Headers headers)
    {
        if (!string.IsNullOrWhiteSpace(appConfiguration.BlockClusterIngressIp) &&
            !string.IsNullOrWhiteSpace(appConfiguration.BlockAddressSuffix))
        {
            headers[Constants.Headers.Host] = baseUri.Host;
            baseUri = new UriBuilder(baseUri)
            {
                Host = appConfiguration.BlockClusterIngressIp
            }.Uri;
        }
    }

    private static QueryString BuildQueryString(Uri? originUri, Guid? nodeId, Guid? entryId)
    {
        var queryString = originUri is not null
            ? new QueryString(originUri.Query)
            : new QueryString();

        if (nodeId.HasValue)
        {
            queryString = AppendNodeQuerystringValues(queryString, nodeId.Value, entryId);
        }

        return queryString;
    }

    private static Uri BuildUri(Uri baseUri, string path, QueryString queryString)
    {
        return new UriBuilder(baseUri)
        {
            Path = path,
            Query = queryString.ToString()
        }.Uri;
    }

    private static QueryString AppendNodeQuerystringValues(QueryString originQueryString, Guid nodeId, Guid? entryId)
    {
        var newOriginQueryString = HttpUtility.ParseQueryString(originQueryString.ToString());

        newOriginQueryString.Remove("nodeId"); //prevent injection
        newOriginQueryString.Remove("entryId"); //prevent injection

        newOriginQueryString.Add("nodeId", nodeId.ToString());

        if (entryId.HasValue)
        {
            newOriginQueryString.Add("entryId", entryId.ToString());
        }

        return QueryString.FromUriComponent("?" + newOriginQueryString);
    }
}