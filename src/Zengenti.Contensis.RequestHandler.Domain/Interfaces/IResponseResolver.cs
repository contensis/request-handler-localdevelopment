using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces
{
    public interface IResponseResolver
    {
        Task<string> Resolve(string content, RouteInfo routeInfo, int currentDepth, CancellationToken ct);
    }
}