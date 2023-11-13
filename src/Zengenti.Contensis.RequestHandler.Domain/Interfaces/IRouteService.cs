using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

public interface IRouteService
{
    Task<RouteInfo> GetRouteForRequest(HttpRequest request, Headers headers);
}