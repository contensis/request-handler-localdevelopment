using Grpc.Core;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Extensions;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Services;

public class CorePublishingService : ICorePublishingService
{
    private readonly IRequestContext _requestContext;
    private readonly IPublishingServiceCache _cache;
    private readonly IPublishingApi _publishingApi;
    private readonly IRouteInfoFactory _routeInfoFactory;
    private readonly ICacheKeyService _cacheKeyService;
    private readonly ILogger<CorePublishingService> _logger;
    private readonly IServerTypeResolver _serverTypeResolver;

    public CorePublishingService(
        IRequestContext requestContext,
        IPublishingServiceCache cache,
        ICacheKeyService cacheKeyService,
        IServerTypeResolver serverTypeResolver,
        IPublishingApi publishingApi,
        IRouteInfoFactory routeInfoFactory,
        ILogger<CorePublishingService> logger)
    {
        _serverTypeResolver = serverTypeResolver;
        _requestContext = requestContext;
        _cache = cache;
        _cacheKeyService = cacheKeyService;
        _publishingApi = publishingApi;
        _routeInfoFactory = routeInfoFactory;
        _logger = logger;
    }

    public async Task<BlockVersionInfo?> GetBlockVersionInfo(Guid blockVersionId)
    {
        var blockVersionInfo = _cache.GetBlockVersionInfo(blockVersionId);
        if (blockVersionInfo != null)
        {
            return blockVersionInfo;
        }

        blockVersionInfo = await _publishingApi.GetBlockVersionInfo(blockVersionId);
        if (blockVersionInfo == null)
        {
            return null;
        }

        blockVersionInfo.EnsureDefaultStaticPaths();

        _cache.SetBlockVersionInfo(blockVersionInfo);

        return blockVersionInfo;
    }

    public async Task<RouteInfo?> GetRouteInfoForRequest(
        Guid projectUuid,
        bool isPartialMatchPath,
        Uri originUri,
        Headers headers,
        Node? node = null,
        Guid? contentTypeId = null,
        string? rendererId = null,
        Guid? proxyId = null,
        string? language = null)
    {
        try
        {
            var requestContext = new RequestContext(projectUuid)
            {
                RendererId = rendererId ?? "",
                ContentTypeId = contentTypeId,
                ProxyId = proxyId,
                Language = language ?? "",
                IsPartialMatchPath = isPartialMatchPath,
                BlockVersionConfig = _requestContext.BlockConfig,
                ProxyVersionConfig = _requestContext.ProxyConfig,
                RendererVersionConfig = _requestContext.RendererConfig,
                ServerType = _serverTypeResolver.GetServerType()
            };

            var clientResult = await _publishingApi.GetEndpointForRequest(requestContext);

            if (clientResult == null)
            {
                return null;
            }

            var routeInfo = BuildRouteInfoForRequest(clientResult, originUri, headers, projectUuid, node);

            return routeInfo;
        }
        catch (RpcException e)
        {
            var logLevel = LogLevel.Error;
            if (e.StatusCode == StatusCode.NotFound)
            {
                logLevel = LogLevel.Warning;
            }

            _logger.Log(
                logLevel,
                e,
                "GetEndpointForRequest failed with RpcException.Message {Message} for Uri {Uri}",
                e.Message,
                originUri);
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "GetEndpointForRequest failed with RpcException.Message {Message} for Uri {Uri}",
                e.Message,
                originUri);
            throw;
        }
    }

    public Task<RouteInfo?> GetRouteInfoForRequest(Guid projectUuid, Headers headers, string rendererId, Uri originUri)
    {
        return GetRouteInfoForRequest(projectUuid, false, originUri, headers, rendererId: rendererId);
    }

    public RouteInfo BuildRouteInfoForRequest(
        EndpointRequestInfo endpointRequestInfo,
        Uri originUri,
        Headers headers,
        Guid projectUuid,
        Node? node)
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
                endpointRequestInfo.StaticPaths,
                endpointRequestInfo.BlockVersionNo);

            routeInfo = _routeInfoFactory.Create(
                uri,
                originUri,
                headers,
                node,
                blockVersionInfo,
                endpointRequestInfo.EndpointId,
                endpointRequestInfo.LayoutRendererId);

            blockVersionInfo.EnsureDefaultStaticPaths();

            // Cache the blockVersionInfo to allow quick lookups for paths re-written within static files (e.g. js, css).
            _cache.SetBlockVersionInfo(blockVersionInfo);
        }
        else
        {
            routeInfo = _routeInfoFactory.Create(
                new Uri(endpointRequestInfo.Uri),
                originUri,
                headers,
                node);
        }

        _cacheKeyService.AddRange(endpointRequestInfo.CacheKeys);

        return routeInfo;
    }
}