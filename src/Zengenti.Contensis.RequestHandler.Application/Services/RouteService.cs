using System.Diagnostics;
using Grpc.Core;
using Microsoft.AspNetCore.Http.Extensions;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Services;

public class RouteService(
    AppConfiguration appConfiguration,
    INodeService nodeService,
    IPublishingService publishingService,
    IRouteInfoFactory routeInfoFactory,
    IRequestContext requestContext,
    ICacheKeyService cacheKeyService,
    ILogger<RouteService> logger)
    : IRouteService
{
    private readonly ILogger _logger = logger;

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
        var shouldGetNode = ShouldGetNode(originPath);
        NodeInfo? nodeInfo = null;
        if (shouldGetNode)
        {
            node = await nodeService.GetByPath(originPath);
        }

        nodeLookupTimer.Stop();

        try
        {
            RouteInfo? routeInfo;

            if (node == null)
            {
                routeInfo = await GetRouteInfoForNonNodePath(originUri, headers, originPath);
                routeInfo.Metrics.Add("nodeLookup", nodeLookupTimer.ElapsedMilliseconds);

                routeInfo.DebugData.NodeCheckResult = shouldGetNode
                    ? $"A node was not found for path {originPath}"
                    : $"Path {originPath} is excluded from node checks";

                return routeInfo;
            }

            CheckAndSetNodeHeaders(headers, node);

            var routeInfoRequestTimer = new Stopwatch();
            routeInfoRequestTimer.Start();

            nodeInfo = new NodeInfo(node.Id.Value, node.EntryId, node.Path);
            ProxyInfo? proxyInfo = null;
            if (node.ProxyRef != null)
            {
                var isPartialMatchPath = !node.Path.EqualsCaseInsensitive(originPath);

                if (!isPartialMatchPath || node.ProxyRef.PartialMatch)
                {
                    proxyInfo = new ProxyInfo(node.ProxyRef.Id, node.ProxyRef.ParseContent, isPartialMatchPath);
                }
            }

            // We have a node so need to understand what to invoke (block or proxy)
            routeInfo = await publishingService.GetRouteInfoForRequest(
                requestContext.ProjectUuid,
                originUri,
                headers,
                nodeInfo,
                node.ContentTypeId,
                node.RendererRef?.Uuid == Guid.Empty
                    ? node.RendererRef?.Id
                    : node.RendererRef?.Uuid.ToString(),
                proxyInfo,
                node.Language);

            routeInfoRequestTimer.Stop();

            if (routeInfo != null)
            {
                // Ensure the node cache keys are included in the final response
                cacheKeyService.AddRange(node.CacheKeys);

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
            var logMessagePrefix = "RpcException in GetRouteForRequest";
            if (e.StatusCode == StatusCode.NotFound)
            {
                logLevel = LogLevel.Information;
                logMessagePrefix = "No block/proxy returned by GetRouteForRequest";
            }

            if (e.Data.Contains(Constants.Exceptions.DataKeyForOriginalMessage))
            {
                _logger.Log(
                    logLevel,
                    e,
                    "{LogMessagePrefix} with message {Message} for path {Path} . Initial message: {InitialMessage}",
                    logMessagePrefix,
                    e.Message,
                    originPath,
                    e.Data[Constants.Exceptions.DataKeyForOriginalMessage]);
            }
            else
            {
                _logger.Log(
                    logLevel,
                    e,
                    "{LogMessagePrefix} with message {Message} for path {Path}",
                    logMessagePrefix,
                    e.Message,
                    originPath);
            }
        }
        catch (Exception e)
        {
            if (e.Data.Contains(Constants.Exceptions.DataKeyForOriginalMessage))
            {
                _logger.LogError(
                    e,
                    "Exception in GetRouteForRequest with message {Message} for path {Path} . Initial message: {InitialMessage}",
                    e.Message,
                    originPath,
                    e.Data[Constants.Exceptions.DataKeyForOriginalMessage]);
            }
            else
            {
                _logger.LogError(
                    e,
                    "GetRouteForRequest Exception with message {Message} for path {Path}",
                    e.Message,
                    originPath);
            }
        }

        // return empty route
        var notFoundRoute = routeInfoFactory.CreateNotFoundRoute(headers, nodeInfo?.Path ?? "");
        notFoundRoute.DebugData.NodeInfo = nodeInfo;
        return notFoundRoute;
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
            blockVersionInfo = await publishingService.GetBlockVersionInfo(staticPath.BlockVersionId);
            staticBlockVersionInfoFetch.Stop();
            staticBlockVersionInfoFetchMs = staticBlockVersionInfoFetch.ElapsedMilliseconds;
        }

        var returnInfo = routeInfoFactory.CreateForNonNodePath(
            originUri,
            headers,
            blockVersionInfo);

        if (staticBlockVersionInfoFetchMs.HasValue)
        {
            returnInfo?.Metrics.Add("staticBlockVersionLookup", staticBlockVersionInfoFetchMs.Value);
        }

        return returnInfo;
    }

    private bool ShouldGetNode(string path)
    {
        // Don't like this hard-coded path, maybe move to config?
        // We can negate anything that is a rewritten static path

        var pathIsRewritten = StaticPath.Parse(path)?.IsRewritten ?? false;
        if (pathIsRewritten)
        {
            return false;
        }

        if (path.ToLowerInvariant() == "/favicon.ico" ||
            path.StartsWithCaseInsensitive("/contensis-preview-toolbar/") ||
            Constants.Paths.PassThroughPrefixes.Any(path.StartsWithCaseInsensitive))
        {
            return false;
        }

        if (Constants.Paths.ApiPrefixes.Any(path.StartsWithCaseInsensitive) &&
            appConfiguration.AliasesWithApiRoutes?.ContainsCaseInsensitive(requestContext.Alias) != true)
        {
            return false;
        }

        return true;
    }

    private void CheckAndSetProjectHeaders(Headers headers)
    {
        if (headers.Debug || headers.HasKey(Constants.Headers.RequiresAlias))
        {
            CallContext.Current[Constants.Headers.RequiresAlias] = requestContext.Alias;
        }

        if (headers.Debug || headers.HasKey(Constants.Headers.RequiresProjectApiId))
        {
            CallContext.Current[Constants.Headers.RequiresProjectApiId] = requestContext.ProjectApiId;
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

        if (blockVersionInfo.VersionNo != null)
        {
            CallContext.Current[Constants.Headers.RequiresVersionNo] = blockVersionInfo.VersionNo.ToString();
        }
    }
}