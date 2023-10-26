using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;
using Renderer = Zengenti.Contensis.RequestHandler.LocalDevelopment.Models.Renderer;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

/// <summary>
/// This is the initial version of request handler standalone (configuration file based) service.
/// </summary>
[Obsolete("Use LocalDevPublishingService instead")]
public class SiteConfigPublishingService : ILocalDevPublishingService
{
    private readonly ISiteConfigLoader _siteConfigLoader;
    private readonly IRouteInfoFactory _routeInfoFactory;
    private readonly ICacheKeyService _cacheKeyService;
    private readonly bool _enableFullUriRouting;

    public SiteConfigPublishingService(
        ISiteConfigLoader siteConfigLoader,
        IRouteInfoFactory routeInfoFactory,
        ICacheKeyService cacheKeyService,
        bool enableFullUriRouting = false)
    {
        _enableFullUriRouting = enableFullUriRouting;
        _siteConfigLoader = siteConfigLoader;
        _routeInfoFactory = routeInfoFactory;
        _cacheKeyService = cacheKeyService;
    }

    public Task<RouteInfo?> GetRouteInfoForRequest(
        Guid projectUuid,
        bool isPartialMatchPath,
        Uri originUri,
        Headers headers,
        Node? node,
        Guid? contentTypeId = null,
        string? rendererId = null,
        Guid? proxyId = null,
        string? language = null)
    {
        Renderer? renderer = null;
        Proxy? proxy = null;
        var hasProxyIdSet = proxyId.HasValue && proxyId != Guid.Empty;

        // Mimic execution performed in Renderer service
        if (isPartialMatchPath && hasProxyIdSet)
        {
            proxy = _siteConfigLoader.SiteConfig.GetProxyByUuid(proxyId!.Value);
        }
        else if (!string.IsNullOrWhiteSpace(rendererId) &&
                 (!Guid.TryParse(rendererId, out var rendererUuid) || rendererUuid != Guid.Empty))
        {
            if (rendererUuid != Guid.Empty)
            {
                renderer = _siteConfigLoader.SiteConfig.GetRendererByUuid(rendererUuid);
            }
            else
            {
                renderer = _siteConfigLoader.SiteConfig.GetRendererById(rendererId);
            }
        }
        else if (contentTypeId.HasValue)
        {
            renderer = _siteConfigLoader.SiteConfig.GetRendererByContentTypeUuid(contentTypeId);
        }
        else if (hasProxyIdSet)
        {
            proxy = _siteConfigLoader.SiteConfig.GetProxyByUuid(proxyId!.Value);
        }

        if (renderer != null)
        {
            var endpointRef = renderer?.ExecuteRules();
            if (endpointRef != null)
            {
                var block = _siteConfigLoader.SiteConfig.GetBlockByUuid(endpointRef.BlockUuid);

                var endpoint = block.Endpoints.SingleOrDefault(e =>
                    e.Id.EqualsCaseInsensitive(endpointRef.EndpointId));
                var blockVersionInfo = new BlockVersionInfo(projectUuid, block.Id!, block.Uuid, block.BaseUri!,
                    block.Branch,
                    _enableFullUriRouting, block.StaticPaths, block.VersionNo);

                var routeInfo = _routeInfoFactory.Create(
                    endpoint!.Uri,
                    originUri,
                    headers,
                    node,
                    blockVersionInfo,
                    endpointRef.EndpointId,
                    renderer?.LayoutRendererId);

                SetCacheKeys(block.Uuid, block.Uuid, renderer!.Uuid, renderer!.LayoutRendererId);
                return Task.FromResult(routeInfo)!;
            }
        }

        if (proxy != null)
        {
            var baseUri = new UriBuilder(proxy.Server) { Path = originUri.AbsolutePath, Query = originUri.Query }
                .Uri;
            var routeInfo = _routeInfoFactory.Create(baseUri, originUri, new Headers(), node);
            return Task.FromResult(routeInfo)!;
        }

        return Task.FromResult<RouteInfo>(null!)!;
    }


    public Task<RouteInfo?> GetRouteInfoForRequest(Guid projectUuid, Headers headers, string rendererId,
        Uri originUri)
    {
        var renderer = _siteConfigLoader.SiteConfig.GetRendererById(rendererId);
        var endpointRef = renderer?.ExecuteRules();

        if (endpointRef != null)
        {
            var block = _siteConfigLoader.SiteConfig.GetBlockByUuid(endpointRef.BlockUuid);
            var endpoint = block.Endpoints.SingleOrDefault(e => e.Id.EqualsCaseInsensitive(endpointRef.EndpointId));
            var blockVersionInfo = new BlockVersionInfo(projectUuid, block.Id!, block.Uuid, block.BaseUri!,
                block.Branch,
                _enableFullUriRouting, block.StaticPaths, block.VersionNo);

            var routeInfo = _routeInfoFactory.Create(
                endpoint!.Uri,
                originUri,
                new Headers(),
                null,
                blockVersionInfo,
                endpointRef.EndpointId,
                renderer?.LayoutRendererId);

            SetCacheKeys(block.Uuid, block.Uuid, renderer.Uuid, null);

            return Task.FromResult(routeInfo)!;
        }

        return null!;
    }

    public Task<BlockVersionInfo?> GetBlockVersionInfo(Guid blockVersionId)
    {
        var block = _siteConfigLoader.SiteConfig.Blocks.FirstOrDefault(b => b.Uuid == blockVersionId);
        if (block != null)
        {
            var projectUuid = Guid.Empty; // NOT required for local development ATM.
            var versionInfo = new BlockVersionInfo(projectUuid, block.Id!, block.Uuid, block.BaseUri!, block.Branch,
                _enableFullUriRouting, block.StaticPaths, block.VersionNo);
            return Task.FromResult(versionInfo)!;
        }

        return null!;
    }

    public RouteInfo? BuildRouteInfoForRequest(EndpointRequestInfo endpointRequestInfo, Uri originUri,
        Headers headers, Guid projectUuid, Node? node = null)
    {
        throw new NotImplementedException();
    }


    public Block GetBlockById(string id)
    {
        return _siteConfigLoader.SiteConfig!.GetBlockById(id);
    }

    public Guid? GetContentTypeUuid(string id)
    {
        return _siteConfigLoader.SiteConfig!.ContentTypeRendererMap.FirstOrDefault(m => m.ContentTypeId == id)
            ?.ContentTypeUuid;
    }

    private void SetCacheKeys(Guid blockId, Guid blockVersionId, Guid rendererId, Guid? layoutRendererId)
    {
        _cacheKeyService.Add(blockId.ToString());
        _cacheKeyService.Add(blockVersionId.ToString());
        _cacheKeyService.Add(rendererId.ToString());

        if (layoutRendererId.HasValue)
        {
            _cacheKeyService.Add(layoutRendererId.Value.ToString());
        }
    }
}