using System.Net.Http.Headers;
using System.Text.Json;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Middleware;

public static class ExceptionHandler
{
    public static async Task<bool> HandlePageletExceptions(HttpContext context, AggregateException aggregateException)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = new MediaTypeHeaderValue(Constants.ContentTypes.ApplicationJson).ToString();

        var innerExceptionDataObjects = new List<object>();

        GatherExceptionData(aggregateException, innerExceptionDataObjects);

        if (innerExceptionDataObjects.Count == 0)
        {
            return false;
        }

        var exceptionsAsJson = JsonSerializer.Serialize(
            innerExceptionDataObjects,
            AppJsonSerializerContext.Default.ListObject);

        await context.Response.WriteAsync(exceptionsAsJson);
        return true;
    }

    private static void GatherExceptionData(
        AggregateException? aggregateException,
        List<object> innerExceptionDataObjects)
    {
        if (aggregateException == null)
        {
            return;
        }

        foreach (var innerException in aggregateException.InnerExceptions)
        {
            if (innerException is EndpointException endpointException)
            {
                innerExceptionDataObjects.Add(new EndpointExceptionData(endpointException));
            }

            if (innerException is EndpointRecursionException endpointRecursionException)
            {
                innerExceptionDataObjects.Add(new EndpointExceptionData(endpointRecursionException));
            }

            if (innerException is AggregateException innerAggregateException)
            {
                GatherExceptionData(innerAggregateException, innerExceptionDataObjects);
            }
        }
    }
}