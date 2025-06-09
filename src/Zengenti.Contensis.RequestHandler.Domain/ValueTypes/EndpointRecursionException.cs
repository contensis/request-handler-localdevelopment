namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

[Serializable]
public class EndpointRecursionException(RouteInfo routeInfo, string? message = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public RouteInfo RouteInfo { get; } = routeInfo;
}