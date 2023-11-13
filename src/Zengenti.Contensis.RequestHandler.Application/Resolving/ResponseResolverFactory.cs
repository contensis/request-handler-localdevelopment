using Zengenti.Contensis.RequestHandler.Domain.Interfaces;

namespace Zengenti.Contensis.RequestHandler.Application.Resolving;

public class ResponseResolverFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ResponseResolverFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IResponseResolver GetResolver(HttpResponseMessage responseMessage)
    {
        if (responseMessage.Content.Headers.TryGetValues("Content-Type", out var values))
        {
            if (values.Any(v => v.StartsWithCaseInsensitive("text/html")))
            {
                // This is disgusting, but only way around a circular dependency injection issue.
                var endpointRequestService = _serviceProvider.GetService<IEndpointRequestService>();
                var resolver = _serviceProvider.GetService<HtmlResponseResolver>();
                resolver!.RequestService = endpointRequestService;
                return resolver;
            }
        }

        return _serviceProvider.GetService<GenericResponseResolver>()!;
    }
}