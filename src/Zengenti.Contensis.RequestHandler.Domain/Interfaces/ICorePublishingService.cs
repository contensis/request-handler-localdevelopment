using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

/// <summary>
///     An interface for GRPC or HTTP based publishing service instances
/// </summary>
public interface ICorePublishingService
{
    Task<RouteInfo?> GetRouteInfoForRequest(
        Guid projectUuid,
        Uri originUri,
        Headers headers,
        NodeInfo? nodeInfo = null,
        Guid? contentTypeId = null,
        string? rendererId = null,
        ProxyInfo? proxyInfo = null,
        string? language = null);

    Task<RouteInfo?> GetRouteInfoForRequest(Guid projectUuid, Headers headers, string rendererId, Uri originUri);

    Task<BlockVersionInfo?> GetBlockVersionInfo(Guid blockVersionId);

    RouteInfo? BuildRouteInfoForRequest(
        EndpointRequestInfo endpointRequestInfo,
        Uri originUri,
        Headers headers,
        Guid projectUuid,
        NodeInfo? nodeInfo = null,
        ProxyInfo? proxyInfo = null);
}