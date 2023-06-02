namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

[Serializable]
public class EndpointRecursionException : Exception
{
    public EndpointRecursionException(RouteInfo routeInfo, string? message = null, Exception? innerException = null)
        : base(message, innerException)
    {
        RouteInfo = routeInfo;
    }

    public RouteInfo RouteInfo { get; }
}