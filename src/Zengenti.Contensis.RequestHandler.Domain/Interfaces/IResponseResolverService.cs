using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces
{
    public interface IResponseResolverService
    {
        Task<string?> Resolve(HttpResponseMessage response, RouteInfo routeInfo, int currentDepth, CancellationToken ct);
    }
}