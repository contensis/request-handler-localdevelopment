using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Zengenti.Contensis.RequestHandler.Application.Services;

public class EndpointRequestService : IEndpointRequestService
{
    private readonly IResponseResolverService _responseResolverService;
    private readonly IRequestContext _requestContext;
    private readonly ICacheKeyService _cacheKeyService;
    private readonly ILogger<EndpointRequestService> _logger;

    public static readonly string[] DisallowedRequestHeaderMappings =
    {
        "Host", // Will be explicitly set
        "Accept-Encoding", // Don't compress endpoint traffic as compression will be done in Varnish
        "version", // Don't pass on a version header because we have to set a version header to blocks them selves
        "x-requires-depends",
        "x-ssl",
        "x-internal-host",
        "use-app-servers",
        "x-varnish-authentication",
        "x-authcache-get-key",
        "x-authcache-get-key-stage",
        "x-varnish",
        "contensis-classic-version",
        "x-site-type",
        "x-block-config",
        "x-proxy-config",
        "x-renderer-config",
        "x-iis-hostname",
        "x-loadbalancer-vip",
        "x-project-uuid",
        "x-project-api-id",
        "branch",
        "version",
        "traceparent",
        "x-forwarded-proto"
    };

    public static readonly string[] DisallowedRequestHeaders =
    {
        "x-requires-depends",
        "x-ssl",
        "x-internal-host",
        "use-app-servers",
        "x-varnish-authentication",
        "x-authcache-get-key",
        "x-authcache-get-key-stage",
        "x-varnish",
        "contensis-classic-version",
        "x-site-type",
        "x-block-config",
        "x-proxy-config",
        "x-renderer-config",
        "x-iis-hostname",
        "x-loadbalancer-vip",
        "x-project-uuid",
        "x-project-api-id",
        "branch",
        "version",
        "traceparent",
        "x-forwarded-proto"
    };

    private IHttpClientFactory _clientFactory;

    public EndpointRequestService(
        IHttpClientFactory clientFactory,
        IResponseResolverService responseResolverService,
        IRequestContext requestContext,
        ICacheKeyService cacheKeyService,
        ILogger<EndpointRequestService> logger)
    {
        _clientFactory = clientFactory;
        _responseResolverService = responseResolverService;
        _requestContext = requestContext;
        _cacheKeyService = cacheKeyService;
        _logger = logger;
    }

    public async Task<EndpointResponse> Invoke(
        HttpMethod httpMethod,
        Stream? content,
        Dictionary<string, IEnumerable<string>>? headers,
        RouteInfo routeInfo,
        int currentDepth,
        CancellationToken cancellationToken)
    {
        RecursionChecker.Check(currentDepth, routeInfo);

        var measurer = new PageletPerformanceMeasurer(_requestContext.TraceEnabled, autoStart: true);

        _logger.LogInformation("Making request to {Uri}", routeInfo.Uri);

        bool isStreamingRequestMessage = routeInfo.BlockVersionInfo != null &&
                                         (httpMethod == HttpMethod.Post ||
                                          httpMethod == HttpMethod.Put ||
                                          httpMethod == HttpMethod.Patch);
        using var targetRequestMessage =
            await CreateRequestMessage(httpMethod, content, headers, routeInfo, isStreamingRequestMessage);


        try
        {
            var httpClient = _clientFactory.CreateClient("no-auto-redirect");
            var requestTimeoutInMinutes = 6 * 10;
            if (isStreamingRequestMessage && httpClient.Timeout.TotalMinutes < requestTimeoutInMinutes)
            {
                httpClient.Timeout = new TimeSpan(0, requestTimeoutInMinutes, 0);
            }

            using var responseMessage = await httpClient.SendAsync(targetRequestMessage,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            measurer.EndOfRequest();


            var endpointResponse = await GetContent(routeInfo, responseMessage, currentDepth, cancellationToken,
                measurer, targetRequestMessage.RequestUri, targetRequestMessage.Method, targetRequestMessage.Headers);

            if (!endpointResponse.IsErrorStatusCode())
            {
                return endpointResponse;
            }

            var absoluteUri = routeInfo.Uri.AbsoluteUri;

            if (absoluteUri.EndsWithCaseInsensitive("/favicon.ico"))
            {
                return endpointResponse;
            }

            using (_logger.BeginScope(new
                   {
                       requestContext = JsonSerializer.Serialize(_requestContext),
                       routeInfo = JsonSerializer.Serialize(routeInfo)
                   }))
            {
                _logger.LogWarning(
                    "Invoking endpoint {AbsoluteUri} was not successful: {StatusCode}",
                    absoluteUri, endpointResponse.StatusCode);
            }

            return endpointResponse;
        }
        catch (Exception e)
        {
            using (_logger.BeginScope(new
                   {
                       requestContext = JsonSerializer.Serialize(_requestContext),
                       routeInfo = JsonSerializer.Serialize(routeInfo)
                   }))
            {
                _logger.LogError(e,
                    $"Failed to invoke endpoint {routeInfo.Uri} with http method {httpMethod.Method} .");
            }

            throw;
        }
    }

    private async Task<EndpointResponse> GetContent(
        RouteInfo routeInfo,
        HttpResponseMessage responseMessage,
        int currentDepth,
        CancellationToken ct,
        PageletPerformanceMeasurer measurer,
        Uri? requestUri,
        HttpMethod requestMethod,
        HttpRequestHeaders requestMessageHeaders)
    {
        var responseHeaders = GetResponseHeaders(responseMessage);

        _cacheKeyService.Add(responseHeaders);

        if (responseMessage.IsResponseResolvable() &&
            (routeInfo.ParseContent || routeInfo.BlockVersionInfo?.BlockVersionId != null))
        {
            var resolvedContent =
                await _responseResolverService.Resolve(responseMessage, routeInfo, currentDepth, ct);

            measurer.EndOfParsing();
            measurer.End();

            return new EndpointResponse(
                resolvedContent,
                responseHeaders,
                (int)responseMessage.StatusCode,
                _requestContext.TraceEnabled
                    ? new PageletPerformanceData(measurer, requestUri, requestMethod, requestMessageHeaders)
                    : null);
        }

        var ms = new MemoryStream();
        await responseMessage.Content.CopyToAsync(ms, ct);
        measurer.EndOfParsing();
        measurer.End();

        return new EndpointResponse(
            ms,
            responseHeaders,
            (int)responseMessage.StatusCode,
            _requestContext.TraceEnabled
                ? new PageletPerformanceData(measurer, requestUri, requestMethod, requestMessageHeaders)
                : null);
    }

    private async Task<HttpRequestMessage> CreateRequestMessage(
        HttpMethod httpMethod,
        Stream? content,
        Dictionary<string, IEnumerable<string>>? headers,
        RouteInfo routeInfo,
        bool isStreamingRequestMessage)
    {
        var requestMessage = new HttpRequestMessage
        {
            RequestUri = routeInfo.Uri,
            Method = httpMethod
        };

        if (content != null)
        {
            if (isStreamingRequestMessage)
            {
                requestMessage.Content = new StreamContent(content);
            }
            else
            {
                if (!(content is MemoryStream ms))
                {
                    ms = new MemoryStream();
                    await content.CopyToAsync(ms);

                    ms.Position = 0;
                }

                requestMessage.Content = new StreamContent(ms);
            }
        }

        AddHeaders(routeInfo, requestMessage, headers);

        return requestMessage;
    }

    private static void AddHeaders(RouteInfo routeInfo, HttpRequestMessage requestMessage,
        Dictionary<string, IEnumerable<string>>? headers)
    {
        if (headers == null)
        {
            return;
        }

        foreach (var (key, value) in headers)
        {
            if ("key" == "x-requires-depends" && routeInfo.IsIisFallback)
            {
                requestMessage.Headers.TryAddWithoutValidation(key, value);
            }
            else if (!DisallowedRequestHeaderMappings.ContainsCaseInsensitive(key))
            {
                requestMessage.Headers.TryAddWithoutValidation(key, value);
            }
        }

        if (headers.ContainsKey("Content-Type") &&
            MediaTypeHeaderValue.TryParse(string.Join(",", headers["Content-Type"]), out var parsedValue))
        {
            if (requestMessage.Content is not null)
            {
                requestMessage.Content.Headers.ContentType = parsedValue;
            }
        }

        // requestMessage.Headers.TryAddWithoutValidation("branch",
        //     routeInfo.BlockVersionInfo?.Branch);

        // requestMessage.Headers.TryAddWithoutValidation("version",
        //     routeInfo.BlockVersionInfo?.VersionNo.ToString());

        if (routeInfo.IsIisFallback && headers.ContainsKey(Constants.Headers.IisHostName))
        {
            // Override host header with IIS fallback host
            requestMessage.Headers.Host = headers[Constants.Headers.IisHostName].FirstOrDefault();
        }
        else
        {
            var host = routeInfo.Headers.GetFirstValueIfExists(HeaderNames.Host);

            requestMessage.Headers.Host = host ?? routeInfo.Uri.Host;
        }
    }

    private static Headers GetResponseHeaders(HttpResponseMessage responseMessage)
    {
        // Responses have headers in both .Headers and .Content.Headers
        var headers = new Headers(responseMessage.Headers);

        foreach (var (key, value) in responseMessage.Content.Headers)
        {
            headers.Add(key, value);
        }

        return headers;
    }
}