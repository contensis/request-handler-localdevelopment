using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Domain.Common;

public static class RecursionChecker
{
    public static void Check(int depth, RouteInfo routeInfo)
    {
        if (depth > 9)
        {
            throw new EndpointRecursionException(
                routeInfo,
                $"Reached max recursion depth: {depth} for endpoint id: {routeInfo.EndpointId} and path: {routeInfo.Uri?.PathAndQuery}");
        }
    }
}