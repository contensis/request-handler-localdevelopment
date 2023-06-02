namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

[Serializable]
public class EndpointException : Exception
{
    public EndpointException(
        RouteInfo? endpoint,
        EndpointResponse endpointResponse,
        string? message = null,
        Exception? innerException = null
    )
        : base(message, innerException)
    {
        Endpoint = endpoint;
        EndpointResponse = endpointResponse;
    }

    public RouteInfo? Endpoint { get; }

    public EndpointResponse EndpointResponse { get; }
}