using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

public interface IEndpointRequestService
{
    Task<EndpointResponse> Invoke(HttpMethod httpMethod, Stream? content,
        Dictionary<string, IEnumerable<string>>? headers, RouteInfo routeInfo, int currentDepth,
        CancellationToken cancellationToken);
}