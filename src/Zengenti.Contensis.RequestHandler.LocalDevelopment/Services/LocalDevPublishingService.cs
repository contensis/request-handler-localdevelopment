using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public class LocalDevPublishingService : ILocalDevPublishingService
{
    private readonly IPublishingApi _publishingApi;
    private readonly ISiteConfigLoader _siteConfigLoader;
    private readonly IRequestContext _requestContext;
    private readonly ICorePublishingService _corePublishingService;
    private readonly IRouteInfoFactory _routeInfoFactory;

    public LocalDevPublishingService(ISiteConfigLoader siteConfigLoader, IRequestContext requestContext,
        IPublishingApi publishingApi, ICorePublishingService corePublishingService,
        IRouteInfoFactory routeInfoFactory)
    {
        _siteConfigLoader = siteConfigLoader;
        _requestContext = requestContext;
        _publishingApi = publishingApi;
        _corePublishingService = corePublishingService;
        _routeInfoFactory = routeInfoFactory;

        // TODO: load the block version info for overriden blocks
    }

    public async Task<RouteInfo?> GetRouteInfoForRequest(
        Guid projectId,
        bool isPartialMatchPath,
        Uri originUri,
        Headers headers,
        Node? node,
        Guid? contentTypeId = null,
        string? rendererId = null,
        Guid? proxyId = null,
        string? language = null)
    {
        var requestContext = new RequestContext(projectId)
        {
            RendererId = rendererId,
            ContentTypeId = contentTypeId,
            ProxyId = proxyId,
            Language = language ?? "en-GB",
            IsPartialMatchPath = isPartialMatchPath,
            BlockVersionConfig = string.IsNullOrWhiteSpace(_requestContext.BlockConfig)
                ? "block-versionstatus=latest"
                : _requestContext.BlockConfig,
            ProxyVersionConfig = string.IsNullOrWhiteSpace(_requestContext.ProxyConfig)
                ? "proxy-versionstatus=published"
                : _requestContext.ProxyConfig,
            RendererVersionConfig = string.IsNullOrWhiteSpace(_requestContext.RendererConfig)
                ? "renderer-versionstatus=published"
                : _requestContext.RendererConfig,
            ServerType = ServerType.Preview
        };

        var endpointForRequest = await _publishingApi.GetEndpointForRequest(requestContext);

        // check if the block id returned in endpointForRequest is overriden in siteconfig
        // if it is not then return the RouteInfo built in CorePublishingService.GetRouteInfoForRequest
        // if it is then return a new RouteInfo built  using the siteconfig data
        var overridenBlock = _siteConfigLoader.SiteConfig.GetBlockById(endpointForRequest!.BlockId);
        if (overridenBlock == null)
        {
            return _corePublishingService.BuildRouteInfoForRequest(endpointForRequest, originUri, headers,
                projectId, node);
        }

        // TODO: deal with endpoints
        // TODO: check if we need to populate enableFullUriRouting
        var enableFullUriRouting = false;
        var blockVersionInfo = new BlockVersionInfo(projectId, endpointForRequest.BlockId,
            endpointForRequest.BlockVersionId!.Value,
            new Uri(endpointForRequest.Uri), endpointForRequest.Branch, enableFullUriRouting,
            endpointForRequest.StaticPaths,
            endpointForRequest.BlockVersionNo);

        var routeInfo = _routeInfoFactory.Create(
            overridenBlock.BaseUri!,
            originUri,
            new Headers(),
            node,
            blockVersionInfo,
            null,
            endpointForRequest.LayoutRendererId);
        return routeInfo;
    }

    public async Task<RouteInfo?> GetRouteInfoForRequest(Guid projectId, Headers headers, string rendererId,
        Uri originUri)
    {
        return await GetRouteInfoForRequest(projectId, false, originUri, headers, null, null, rendererId);
    }


    public Task<BlockVersionInfo?> GetBlockVersionInfo(Guid blockVersionId)
    {
        return _publishingApi.GetBlockVersionInfo(blockVersionId);
    }

    public RouteInfo? BuildRouteInfoForRequest(EndpointRequestInfo endpointRequestInfo, Uri originUri,
        Headers headers, Guid projectUuid, Node? node)
    {
        return _corePublishingService.BuildRouteInfoForRequest(endpointRequestInfo, originUri, headers, projectUuid,
            node);
    }

    public Guid? GetContentTypeUuid(string id)
    {
        return _siteConfigLoader.SiteConfig.ContentTypeRendererMap.FirstOrDefault(m => m.ContentTypeId == id)
            ?.ContentTypeUuid;
    }

    public Block GetBlockById(string id)
    {
        return _siteConfigLoader.SiteConfig.GetBlockById(id);
    }
}