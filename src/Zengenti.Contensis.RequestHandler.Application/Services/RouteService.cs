using System.Diagnostics;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
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
        var originPath = originUri.AbsolutePath;
        var nodeLookupTimer = new Stopwatch();
        
        nodeLookupTimer.Start();
        var node = ShouldPerformNodeLookup(originPath)
            ? await _nodeService.GetByPath(originPath)
            : null;
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

            var routeInfoRequestTimer = new Stopwatch();
            routeInfoRequestTimer.Start();

            var isPartialMatchPath = node.Path.EqualsCaseInsensitive(originPath) == false;
            var nodeProxyId = node.ProxyRef?.Id;
            if(isPartialMatchPath && node.ProxyRef?.PartialMatch == false)
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
                
                return routeInfo;
            }
        }
        catch (RpcException e)
        {
            if (e.StatusCode == StatusCode.NotFound)
            {
                _logger.LogWarning(e, "Failed to GetRouteInfoForRequest with error {Message}", e.Message);
            }
            else
            {
                _logger.LogError(e, "Failed to GetRouteInfoForRequest with error {Message}", e.Message);
                throw;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to GetRouteInfoForRequest with error {Message}", e.Message);
            throw;
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

        return path.ToLowerInvariant() != "/favicon.ico"
               && !path.StartsWithCaseInsensitive("/api/")
               && !path.StartsWithCaseInsensitive("/contensis-preview-toolbar/")
               && !pathIsRewritten.GetValueOrDefault();
    }
}