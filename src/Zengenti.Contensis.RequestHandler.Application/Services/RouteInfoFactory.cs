using System.Web;
using Microsoft.Net.Http.Headers;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Services;

public class RouteInfoFactory : IRouteInfoFactory
{
    private readonly BlockClusterConfig _blockClusterConfig;
    private readonly IRequestContext _requestContext;

    public RouteInfoFactory(
        IRequestContext requestContext,
        BlockClusterConfig blockClusterConfig)
    {
        _blockClusterConfig = blockClusterConfig;
        _requestContext = requestContext;
    }

    public RouteInfo Create(
        Uri baseUri,
        Uri? originUri,
        Headers headers,
        Node? node = null,
        BlockVersionInfo? blockVersionInfo = null,
        string? endpointId = null,
        Guid? layoutRendererId = null)
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
            parseContent);
    }

    public RouteInfo? CreateForNonNodePath(
        Uri originUri,
        Headers headers,
        BlockVersionInfo? blockVersionInfo = null)
    {
        var path = originUri.AbsolutePath;
        var queryString = BuildQueryString(originUri);

        // Handle API requests
        bool isApiRequest = Constants.Paths.ApiPrefixes.Any(prefix => path.StartsWithCaseInsensitive(prefix)) &&
                            !path.StartsWithCaseInsensitive("/api/publishing/request-handler") &&
                            !path.StartsWithCaseInsensitive("/api/preview-toolbar/blocks");
        if (isApiRequest)
        {
            var apiHost = $"api-{_requestContext.Alias}.cloud.contensis.com";
            var apiUrl = $"https://{apiHost}";
            var apiUri = new Uri(apiUrl);
            var uri = BuildUri(apiUri, path, queryString);
            headers["Host"] = apiHost;
            return new RouteInfo(uri, headers, "", true);
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
                blockVersionInfo);
        }

        return new RouteInfo(
            null,
            headers,
            "",
            false);
    }

    public RouteInfo CreateForIisFallback(Uri originUri, Headers headers)
    {
        var baseUri = new Uri($"https://{_requestContext.LoadBalancerVip}");
        var uri = BuildUri(baseUri, originUri.AbsolutePath, new QueryString(originUri.Query));
        headers["Host"] = _requestContext.IisHostname;

        return new RouteInfo(uri, headers, "", true, isIisFallback: true);
    }

    private void ApplyBlockClusterRouteDetails(ref Uri baseUri, Headers headers)
    {
        if (!string.IsNullOrWhiteSpace(_blockClusterConfig.BlockClusterIngressIp) &&
            !string.IsNullOrWhiteSpace(_blockClusterConfig.BlockAddressSuffix))
        {
            headers[HeaderNames.Host] = baseUri.Host;
            baseUri = new UriBuilder(baseUri)
            {
                Host = _blockClusterConfig.BlockClusterIngressIp
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