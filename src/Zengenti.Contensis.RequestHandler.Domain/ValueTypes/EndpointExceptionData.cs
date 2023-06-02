namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public class EndpointExceptionData
{
    public EndpointExceptionData()
    {
    }

    public EndpointExceptionData(EndpointException endpointException)
    {
        Message = endpointException.Message;
    }

    public EndpointExceptionData(EndpointRecursionException endpointRecursionException)
    {
        Message = endpointRecursionException.Message;
    }

    public string? Message { get; set; }
}