using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

public interface IRouteInfoFactory
{
    RouteInfo Create(
        Uri baseUri,
        Uri originUri,
        Headers headers,
        NodeInfo? nodeInfo = null,
        BlockVersionInfo? blockVersionInfo = null,
        string? endpointId = null,
        Guid? layoutRendererId = null,
        ProxyInfo? proxyInfo = null);

    RouteInfo CreateForNonNodePath(
        Uri originUri,
        Headers headers,
        BlockVersionInfo? blockVersionInfo = null);

    RouteInfo CreateForIisFallback(
        Uri originUri,
        Headers headers,
        RouteInfo? originalRouteInfo);

    RouteInfo CreateNotFoundRoute(Headers headers, string nodePath = "");
}