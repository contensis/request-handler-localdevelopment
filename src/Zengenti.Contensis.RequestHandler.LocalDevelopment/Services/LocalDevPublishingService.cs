using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public class LocalDevPublishingService(
    ISiteConfigLoader siteConfigLoader,
    IRequestContext context,
    IPublishingApi publishingApi,
    ICorePublishingService corePublishingService,
    IRouteInfoFactory routeInfoFactory)
    : ILocalDevPublishingService
{
    // TODO: load the block version info for overriden blocks

    public async Task<RouteInfo?> GetRouteInfoForRequest(
        Guid projectUuid,
        Uri originUri,
        Headers headers,
        NodeInfo? nodeInfo,
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
            Language = language ?? "en-GB",
            IsPartialMatchPath = proxyInfo?.IsPartialMatchPath ?? false,
            BlockVersionConfig = string.IsNullOrWhiteSpace(context.BlockConfig)
                ? "block-versionstatus=live"
                : context.BlockConfig,
            ProxyVersionConfig = string.IsNullOrWhiteSpace(context.ProxyConfig)
                ? "proxy-versionstatus=published"
                : context.ProxyConfig,
            RendererVersionConfig = string.IsNullOrWhiteSpace(context.RendererConfig)
                ? "renderer-versionstatus=published"
                : context.RendererConfig,
            ServerType = ServerType.Preview
        };

        var endpointForRequest = await publishingApi.GetEndpointForRequest(requestContext);

        // check if the block id returned in endpointForRequest is overriden in siteconfig
        // if it is not then return the RouteInfo built in CorePublishingService.GetRouteInfoForRequest
        // if it is then return a new RouteInfo built  using the siteconfig data
        var overridenBlock = siteConfigLoader.SiteConfig.GetBlockById(endpointForRequest!.BlockId);
        if (overridenBlock == null)
        {
            return corePublishingService.BuildRouteInfoForRequest(
                endpointForRequest,
                originUri,
                headers,
                projectUuid,
                nodeInfo,
                proxyInfo);
        }

        // TODO: deal with endpoints
        // TODO: check if we need to populate enableFullUriRouting
        var enableFullUriRouting = false;
        var blockVersionInfo = new BlockVersionInfo(
            projectUuid,
            endpointForRequest.BlockId,
            endpointForRequest.BlockVersionId!.Value,
            new Uri(endpointForRequest.Uri),
            endpointForRequest.Branch,
            enableFullUriRouting,
            endpointForRequest.StaticPaths,
            endpointForRequest.BlockVersionNo);

        var routeInfo = routeInfoFactory.Create(
            overridenBlock.BaseUri!,
            originUri,
            new Headers(),
            nodeInfo,
            blockVersionInfo,
            null,
            endpointForRequest.LayoutRendererId);
        return routeInfo;
    }

    public async Task<RouteInfo?> GetRouteInfoForRequest(
        Guid projectUuid,
        Headers headers,
        string rendererId,
        Uri originUri)
    {
        return await GetRouteInfoForRequest(projectUuid, originUri, headers, null, null, rendererId);
    }

    public Task<BlockVersionInfo?> GetBlockVersionInfo(Guid blockVersionId)
    {
        return publishingApi.GetBlockVersionInfo(blockVersionId);
    }

    public RouteInfo? BuildRouteInfoForRequest(
        EndpointRequestInfo endpointRequestInfo,
        Uri originUri,
        Headers headers,
        Guid projectUuid,
        NodeInfo? nodeInfo,
        ProxyInfo? proxyInfo = null)
    {
        return corePublishingService.BuildRouteInfoForRequest(
            endpointRequestInfo,
            originUri,
            headers,
            projectUuid,
            nodeInfo,
            proxyInfo);
    }

    public Guid? GetContentTypeUuid(string id)
    {
        return siteConfigLoader.SiteConfig.ContentTypeRendererMap.FirstOrDefault(m => m.ContentTypeId == id)
            ?.ContentTypeUuid;
    }

    public Block GetBlockById(string id)
    {
        return siteConfigLoader.SiteConfig.GetBlockById(id)!;
    }
}