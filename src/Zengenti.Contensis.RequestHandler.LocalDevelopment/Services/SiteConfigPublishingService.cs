using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;

#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

/// <summary>
///     This is the initial version of request handler standalone (configuration file based) service.
/// </summary>
[Obsolete("Use LocalDevPublishingService instead")]
public class SiteConfigPublishingService(
    ISiteConfigLoader siteConfigLoader,
    IRouteInfoFactory routeInfoFactory,
    ICacheKeyService cacheKeyService,
    bool enableFullUriRouting = false)
    : ILocalDevPublishingService
{
    public Task<RouteInfo?> GetRouteInfoForRequest(
        Guid projectUuid,
        Uri originUri,
        Headers headers,
        NodeInfo? nodeInfo,
        Guid? contentTypeId = null,
        string? rendererId = null,
        ProxyInfo? proxyInfo = null,
        string? language = null)
    {
        Renderer? renderer = null;
        Proxy? proxy = null;

        // Mimic execution performed in Renderer service
        if (proxyInfo?.IsPartialMatchPath == true)
        {
            proxy = siteConfigLoader.SiteConfig.GetProxyByUuid(proxyInfo.ProxyId);
        }
        else if (!string.IsNullOrWhiteSpace(rendererId) &&
                 (!Guid.TryParse(rendererId, out var rendererUuid) || rendererUuid != Guid.Empty))
        {
            if (rendererUuid != Guid.Empty)
            {
                renderer = siteConfigLoader.SiteConfig.GetRendererByUuid(rendererUuid);
            }
            else
            {
                renderer = siteConfigLoader.SiteConfig.GetRendererById(rendererId);
            }
        }
        else if (contentTypeId.HasValue)
        {
            renderer = siteConfigLoader.SiteConfig.GetRendererByContentTypeUuid(contentTypeId);
        }
        else if (proxyInfo != null)
        {
            proxy = siteConfigLoader.SiteConfig.GetProxyByUuid(proxyInfo.ProxyId);
        }

        if (renderer != null)
        {
            var endpointRef = renderer.ExecuteRules();
            if (endpointRef != null)
            {
                var block = siteConfigLoader.SiteConfig.GetBlockByUuid(endpointRef.BlockUuid);

                var endpoint = block?.Endpoints.SingleOrDefault(
                    e =>
                        e.Id.EqualsCaseInsensitive(endpointRef.EndpointId));
                var blockVersionInfo = new BlockVersionInfo(
                    projectUuid,
                    block?.Id ?? "",
                    block?.Uuid ?? Guid.Empty,
                    block?.BaseUri!,
                    block?.Branch ?? "",
                    enableFullUriRouting,
                    block?.StaticPaths,
                    block?.VersionNo);

                var routeInfo = routeInfoFactory.Create(
                    endpoint!.Uri,
                    originUri,
                    headers,
                    nodeInfo,
                    blockVersionInfo,
                    endpointRef.EndpointId,
                    renderer.LayoutRendererId);

                SetCacheKeys(block.Uuid, block.Uuid, renderer.Uuid, renderer.LayoutRendererId);
                return Task.FromResult(routeInfo)!;
            }
        }

        if (proxy != null)
        {
            var baseUri = new UriBuilder(proxy.Server)
                {
                    Path = originUri.AbsolutePath,
                    Query = originUri.Query
                }
                .Uri;
            var routeInfo = routeInfoFactory.Create(baseUri, originUri, new Headers(), nodeInfo);
            return Task.FromResult(routeInfo)!;
        }

        return Task.FromResult<RouteInfo>(null!)!;
    }

    public Task<RouteInfo?> GetRouteInfoForRequest(
        Guid projectUuid,
        Headers headers,
        string rendererId,
        Uri originUri)
    {
        var renderer = siteConfigLoader.SiteConfig.GetRendererById(rendererId);
        var endpointRef = renderer?.ExecuteRules();

        if (endpointRef != null)
        {
            var block = siteConfigLoader.SiteConfig.GetBlockByUuid(endpointRef.BlockUuid);
            var endpoint = block.Endpoints.SingleOrDefault(e => e.Id.EqualsCaseInsensitive(endpointRef.EndpointId));
            var blockVersionInfo = new BlockVersionInfo(
                projectUuid,
                block.Id!,
                block.Uuid,
                block.BaseUri!,
                block.Branch,
                enableFullUriRouting,
                block.StaticPaths,
                block.VersionNo);

            var routeInfo = routeInfoFactory.Create(
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
        var block = siteConfigLoader.SiteConfig.Blocks.FirstOrDefault(b => b.Uuid == blockVersionId);
        if (block != null)
        {
            var projectUuid = Guid.Empty; // NOT required for local development ATM.
            var versionInfo = new BlockVersionInfo(
                projectUuid,
                block.Id!,
                block.Uuid,
                block.BaseUri!,
                block.Branch,
                enableFullUriRouting,
                block.StaticPaths,
                block.VersionNo);
            return Task.FromResult(versionInfo)!;
        }

        return null!;
    }

    // ReSharper disable once ReturnTypeCanBeNotNullable
    public RouteInfo? BuildRouteInfoForRequest(
        EndpointRequestInfo endpointRequestInfo,
        Uri originUri,
        Headers headers,
        Guid projectUuid,
        NodeInfo? nodeInfo = null,
        ProxyInfo? proxyInfo = null)
    {
        throw new NotImplementedException();
    }

    public Block GetBlockById(string id)
    {
        return siteConfigLoader.SiteConfig.GetBlockById(id);
    }

    public Guid? GetContentTypeUuid(string id)
    {
        return siteConfigLoader.SiteConfig.ContentTypeRendererMap.FirstOrDefault(m => m.ContentTypeId == id)
            ?.ContentTypeUuid;
    }

    private void SetCacheKeys(Guid blockId, Guid blockVersionId, Guid rendererId, Guid? layoutRendererId)
    {
        cacheKeyService.Add(blockId.ToString());
        cacheKeyService.Add(blockVersionId.ToString());
        cacheKeyService.Add(rendererId.ToString());

        if (layoutRendererId.HasValue)
        {
            cacheKeyService.Add(layoutRendererId.Value.ToString());
        }
    }
}