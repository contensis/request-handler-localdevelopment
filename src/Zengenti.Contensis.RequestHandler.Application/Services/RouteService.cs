using System.Diagnostics;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Services;

public class RouteService : IRouteService
{
    private readonly INodeService _nodeService;
    private readonly IPublishingService _publishingService;
    private readonly IRouteInfoFactory _routeInfoFactory;
    private readonly IRequestContext _requestContext;
    private readonly ICacheKeyService _cacheKeyService;
    private readonly ILogger _logger;

    public RouteService(
        INodeService nodeService,
        IPublishingService publishingService,
        IRouteInfoFactory routeInfoFactory,
        IRequestContext requestContext,
        ICacheKeyService cacheKeyService,
        ILogger<RouteService> logger)
    {
        _nodeService = nodeService;
        _publishingService = publishingService;
        _routeInfoFactory = routeInfoFactory;
        _requestContext = requestContext;
        _cacheKeyService = cacheKeyService;
        _logger = logger;
    }

    public async Task<RouteInfo> GetRouteForRequest(HttpRequest request, Headers headers)
    {
        return await GetRouteForRequest(new Uri(request.GetEncodedUrl()), headers);
    }

    public async Task<RouteInfo> GetRouteForRequest(Uri originUri, Headers headers)
    {
        var originPath = originUri.AbsolutePath.Length > 1
            ? originUri.AbsolutePath.TrimEnd('/')
            : originUri.AbsolutePath;

        var nodeLookupTimer = new Stopwatch();

        CheckAndSetProjectHeaders(headers);

        nodeLookupTimer.Start();
        Node? node = null;
        if (ShouldPerformNodeLookup(originPath))
        {
            node = await _nodeService.GetByPath(originPath);
        }

        nodeLookupTimer.Stop();

        try
        {
            RouteInfo? routeInfo;

            if (node == null)
            {
                routeInfo = await GetRouteInfoForNonNodePath(originUri, headers, originPath);
                routeInfo?.Metrics.Add("nodeLookup", nodeLookupTimer.ElapsedMilliseconds);

                return routeInfo;
            }

            CheckAndSetNodeHeaders(headers, node);

            var routeInfoRequestTimer = new Stopwatch();
            routeInfoRequestTimer.Start();

            var isPartialMatchPath = node.Path.EqualsCaseInsensitive(originPath) == false;
            var nodeProxyId = node.ProxyRef?.Id;
            if (isPartialMatchPath && node.ProxyRef?.PartialMatch == false)
            {
                nodeProxyId = null;
            }

            // We have a node so need to understand what to invoke (block or proxy)
            routeInfo = await _publishingService.GetRouteInfoForRequest(
                _requestContext.ProjectUuid,
                isPartialMatchPath: isPartialMatchPath,
                originUri,
                headers,
                node,
                node.ContentTypeId,
                node.RendererRef?.Uuid == Guid.Empty
                    ? node.RendererRef?.Id
                    : node.RendererRef?.Uuid.ToString(),
                nodeProxyId,
                node.Language);

            routeInfoRequestTimer.Stop();

            if (routeInfo != null)
            {
                // Ensure the node cache keys are included in the final response
                _cacheKeyService.AddRange(node.CacheKeys);

                routeInfo.Metrics.Add("nodeLookup", nodeLookupTimer.ElapsedMilliseconds);
                routeInfo.Metrics.Add("getRouteInfoFetch", routeInfoRequestTimer.ElapsedMilliseconds);

                if (routeInfo.BlockVersionInfo != null)
                {
                    CheckAndSetBlockHeaders(headers, routeInfo.BlockVersionInfo);
                }

                return routeInfo;
            }
        }
        catch (RpcException e)
        {
            var logLevel = LogLevel.Error;
            
            if (e.StatusCode == StatusCode.NotFound)
            {
                logLevel = LogLevel.Warning;
            }
            else
            {
                _logger.Log(logLevel, e, "Failed to GetRouteForRequest with RpcException.Message {Message} for url {Uri}", e.Message, originUri);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to GetRouteForRequest with Exception.Message {Message} for url {Uri}", e.Message, originUri);
        }

        var nodePath = "";
        if (node != null)
        {
            nodePath = node.Path;
        }

        var emptyRouteInfo = new RouteInfo(null, headers, nodePath, false);
        return emptyRouteInfo;
    }

    private async Task<RouteInfo?> GetRouteInfoForNonNodePath(Uri originUri, Headers headers, string originPath)
    {
        long? staticBlockVersionInfoFetchMs = null;
        BlockVersionInfo? blockVersionInfo = null;

        var staticPath = StaticPath.Parse(originPath);
        if (staticPath is { IsRewritten: true })
        {
            var staticBlockVersionInfoFetch = new Stopwatch();
            staticBlockVersionInfoFetch.Start();
            blockVersionInfo = await _publishingService.GetBlockVersionInfo(staticPath.BlockVersionId);
            staticBlockVersionInfoFetch.Stop();
            staticBlockVersionInfoFetchMs = staticBlockVersionInfoFetch.ElapsedMilliseconds;
        }

        var returnInfo = _routeInfoFactory.CreateForNonNodePath(
            originUri,
            headers,
            blockVersionInfo);

        if (staticBlockVersionInfoFetchMs.HasValue)
        {
            returnInfo?.Metrics.Add("staticBlockVersionLookup", staticBlockVersionInfoFetchMs.Value);
        }

        return returnInfo;
    }

    private static bool ShouldPerformNodeLookup(string path)
    {
        // Don't like this hard-coded path, maybe move to config?
        // We can negate anything that is a rewritten static path
        var pathIsRewritten = StaticPath.Parse(path)?.IsRewritten;

        bool doLookup = path.ToLowerInvariant() != "/favicon.ico"
                        && !path.StartsWithCaseInsensitive("/contensis-preview-toolbar/")
                        && !pathIsRewritten.GetValueOrDefault()
                        && !Constants.Paths.ApiPrefixes.Any(path.StartsWithCaseInsensitive)
                        && !Constants.Paths.PassThroughPrefixes.Any(path.StartsWithCaseInsensitive);

        return doLookup;
    }

    private void CheckAndSetProjectHeaders(Headers headers)
    {
        if (headers.Debug || headers.HasKey(Constants.Headers.RequiresAlias))
        {
            CallContext.Current[Constants.Headers.RequiresAlias] = _requestContext.Alias;
        }

        if (headers.Debug || headers.HasKey(Constants.Headers.RequiresProjectApiId))
        {
            CallContext.Current[Constants.Headers.RequiresProjectApiId] = _requestContext.ProjectId;
        }
    }

    private void CheckAndSetNodeHeaders(Headers headers, Node node)
    {
        if ((headers.Debug || headers.HasKey(Constants.Headers.RequiresNodeId)) && node.Id != null)
        {
            CallContext.Current[Constants.Headers.RequiresNodeId] = node.Id.ToString();
        }

        if ((headers.Debug || headers.HasKey(Constants.Headers.RequiresEntryId)) && node.EntryId != null)
        {
            CallContext.Current[Constants.Headers.RequiresEntryId] = node.EntryId.ToString();
        }

        if (headers.Debug || headers.HasKey(Constants.Headers.RequiresEntryLanguage))
        {
            CallContext.Current[Constants.Headers.RequiresEntryLanguage] = node.Language;
        }
    }

    private void CheckAndSetBlockHeaders(Headers headers, BlockVersionInfo blockVersionInfo)
    {
        if (headers.Debug || headers.HasKey(Constants.Headers.RequiresBlockId))
        {
            CallContext.Current[Constants.Headers.RequiresBlockId] = blockVersionInfo.BlockId;
        }

        if ((headers.Debug || headers.HasKey(Constants.Headers.RequiresVersionNo)) &&
            blockVersionInfo.VersionNo != null)
        {
            CallContext.Current[Constants.Headers.RequiresVersionNo] = blockVersionInfo.VersionNo.ToString();
        }
    }
}