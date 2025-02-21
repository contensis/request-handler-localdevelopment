using System.Web;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Services;

public class RouteInfoFactory(
    IRequestContext requestContext,
    AppConfiguration appConfiguration)
    : IRouteInfoFactory
{
    public RouteInfo Create(
        Uri baseUri,
        Uri? originUri,
        Headers headers,
        Node? node = null,
        BlockVersionInfo? blockVersionInfo = null,
        string? endpointId = null,
        Guid? layoutRendererId = null,
        Guid? proxyId = null)
    {
        var path = baseUri.AbsolutePath;
        var enableFullUriRouting = blockVersionInfo?.EnableFullUriRouting ?? false;
        var parseContent = false;
        var queryString = BuildQueryString(originUri, node);

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

        if (node?.ProxyRef != null)
        {
            path = originUri != null ? originUri.AbsolutePath : path;
            parseContent = node.ProxyRef.ParseContent;
        }

        var uri = BuildUri(baseUri, path, queryString);

        var nodePath = "";
        if (node != null)
        {
            nodePath = node.Path;
        }

        return new RouteInfo(
            uri,
            headers,
            nodePath,
            true,
            blockVersionInfo,
            endpointId,
            layoutRendererId,
            parseContent,
            blockVersionInfo == null ? proxyId : null)
        {
            DebugData =
            {
                AppConfiguration = appConfiguration,
                Node = node
            }
        };
    }

    public RouteInfo CreateForNonNodePath(
        Uri originUri,
        Headers headers,
        BlockVersionInfo? blockVersionInfo = null)
    {
        var path = originUri.AbsolutePath;
        var queryString = BuildQueryString(originUri);

        // Handle API requests
        var isContensisApiRequest =
            Constants.Paths.ApiPrefixes.Any(prefix => path.StartsWithCaseInsensitive(prefix)) &&
            !path.StartsWithCaseInsensitive("/api/publishing/request-handler") &&
            !path.StartsWithCaseInsensitive("/api/preview-toolbar/blocks") &&
            appConfiguration.AliasesWithApiRoutes?.ContainsCaseInsensitive(requestContext.Alias) != true;
        if (isContensisApiRequest)
        {
            var apiHost = $"api-{requestContext.Alias}.cloud.contensis.com";
            var apiUrl = $"https://{apiHost}";
            var apiUri = new Uri(apiUrl);
            var uri = BuildUri(apiUri, path, queryString);
            headers[Constants.Headers.Host] = apiHost;
            return new RouteInfo(uri, headers, "", true)
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
                uri,
                headers,
                "",
                true,
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
            null,
            headers,
            nodePath,
            false)
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

        return new RouteInfo(uri, headers, "", true, isIisFallback: true)
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

    private static QueryString BuildQueryString(Uri? originUri, Node? node = null)
    {
        var queryString = originUri is not null
            ? new QueryString(originUri.Query)
            : new QueryString();

        if (node is not null)
        {
            queryString = AppendNodeQuerystringValues(queryString, node);
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

    private static QueryString AppendNodeQuerystringValues(QueryString originQueryString, Node node)
    {
        var newOriginQueryString = HttpUtility.ParseQueryString(originQueryString.ToString());

        newOriginQueryString.Remove("nodeId"); //prevent injection
        newOriginQueryString.Remove("entryId"); //prevent injection

        newOriginQueryString.Add("nodeId", node.Id.ToString());

        if (node.EntryId.HasValue)
        {
            newOriginQueryString.Add("entryId", node.EntryId.ToString());
        }

        return QueryString.FromUriComponent("?" + newOriginQueryString);
    }
}