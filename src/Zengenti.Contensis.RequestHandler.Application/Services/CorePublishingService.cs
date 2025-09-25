using System.Runtime.ExceptionServices;
using Grpc.Core;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Extensions;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Services;

public class CorePublishingService(
    IRequestContext context,
    IPublishingServiceCache cache,
    ICacheKeyService cacheKeyService,
    IServerTypeResolver serverTypeResolver,
    IPublishingApi publishingApi,
    IRouteInfoFactory routeInfoFactory)
    : ICorePublishingService
{
    public async Task<BlockVersionInfo?> GetBlockVersionInfo(Guid blockVersionId)
    {
        var blockVersionInfo = cache.GetBlockVersionInfo(blockVersionId);
        if (blockVersionInfo != null)
        {
            return blockVersionInfo;
        }

        blockVersionInfo = await publishingApi.GetBlockVersionInfo(blockVersionId);
        if (blockVersionInfo == null)
        {
            return null;
        }

        blockVersionInfo.EnsureDefaultStaticPaths();

        cache.SetBlockVersionInfo(blockVersionInfo);

        return blockVersionInfo;
    }

    public async Task<RouteInfo?> GetRouteInfoForRequest(
        Guid projectUuid,
        Uri originUri,
        Headers headers,
        NodeInfo? nodeInfo = null,
        Guid? contentTypeId = null,
        string? rendererId = null,
        ProxyInfo? proxyInfo = null,
        string? language = null)
    {
        var requestContext = new RequestContext(projectUuid)
        {
            RendererId = rendererId ?? "",
            ContentTypeId = contentTypeId,
            ProxyId = proxyInfo?.ProxyId,
            Language = language ?? "",
            IsPartialMatchPath = proxyInfo?.IsPartialMatchPath ?? false,
            BlockVersionConfig = context.BlockConfig,
            ProxyVersionConfig = context.ProxyConfig,
            RendererVersionConfig = context.RendererConfig,
            ServerType = serverTypeResolver.GetServerType()
        };

        var messageSuffix =
            $"when calling IPublishingApi.GetEndpointForRequest for requestContext {requestContext.ToJson()}";
        if (originUri != null)
        {
            messageSuffix += $" and path {originUri.AbsolutePath}.";
        }

        ExceptionDispatchInfo? exceptionDispatchInfo;
        try
        {
            var clientResult = await publishingApi.GetEndpointForRequest(requestContext);

            if (clientResult == null)
            {
                return null;
            }

            var routeInfo = BuildRouteInfoForRequest(
                clientResult,
                originUri,
                headers,
                projectUuid,
                nodeInfo,
                proxyInfo);

            return routeInfo;
        }
        catch (RpcException rpcException)
        {
            var message = $"Rpc exception {messageSuffix}";
            if (rpcException.StatusCode == StatusCode.NotFound)
            {
                message = $"Could not resolve a block version or a proxy {messageSuffix}";
            }

            rpcException.Data.Add(Constants.Exceptions.DataKeyForOriginalMessage, message);

            exceptionDispatchInfo = ExceptionDispatchInfo.Capture(rpcException);
        }
        catch (Exception ex)
        {
            var message = $"Exception {messageSuffix}";

            ex.Data.Add(Constants.Exceptions.DataKeyForOriginalMessage, message);
            exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
        }

        exceptionDispatchInfo.Throw();
        return null;
    }

    public Task<RouteInfo?> GetRouteInfoForRequest(Guid projectUuid, Headers headers, string rendererId, Uri originUri)
    {
        return GetRouteInfoForRequest(projectUuid, originUri, headers, rendererId: rendererId);
    }

    public RouteInfo BuildRouteInfoForRequest(
        EndpointRequestInfo endpointRequestInfo,
        Uri originUri,
        Headers headers,
        Guid projectUuid,
        NodeInfo? nodeInfo = null,
        ProxyInfo? proxyInfo = null)
    {
        var uri = new Uri(endpointRequestInfo.Uri);
        RouteInfo routeInfo;
        headers = headers.Merge(new Headers(endpointRequestInfo.Headers));

        if (endpointRequestInfo.BlockVersionId.HasValue)
        {
            var blockVersionInfo = new BlockVersionInfo(
                projectUuid,
                endpointRequestInfo.BlockId,
                endpointRequestInfo.BlockVersionId.Value,
                new Uri(uri.GetLeftPart(UriPartial.Authority)),
                endpointRequestInfo.Branch,
                endpointRequestInfo.EnableFullUriRouting,
                endpointRequestInfo.Pushed.Value,
                endpointRequestInfo.StaticPaths,
                endpointRequestInfo.BlockVersionNo);

            blockVersionInfo.EnsureDefaultStaticPaths();

            // Cache the blockVersionInfo to allow quick lookups for paths re-written within static files (e.g. js, css).
            cache.SetBlockVersionInfo(blockVersionInfo);

            routeInfo = routeInfoFactory.Create(
                uri,
                originUri,
                headers,
                nodeInfo,
                blockVersionInfo,
                endpointRequestInfo.EndpointId,
                endpointRequestInfo.LayoutRendererId);
        }
        else
        {
            routeInfo = routeInfoFactory.Create(
                new Uri(endpointRequestInfo.Uri),
                originUri,
                headers,
                nodeInfo,
                proxyInfo: proxyInfo);
        }

        cacheKeyService.AddRange(endpointRequestInfo.CacheKeys);

        return routeInfo;
    }
}