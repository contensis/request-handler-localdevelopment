using System.Net;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Resolving;

public class ResponseResolverService : IResponseResolverService
{
    private readonly ResponseResolverFactory _resolverFactory;

    public ResponseResolverService(ResponseResolverFactory resolverFactory)
    {
        _resolverFactory = resolverFactory;
    }

    public async Task<string?> Resolve(
        HttpResponseMessage response,
        RouteInfo routeInfo,
        int currentDepth,
        CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotModified)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        return await _resolverFactory.GetResolver(response).Resolve(content, routeInfo, currentDepth, ct);
    }
}