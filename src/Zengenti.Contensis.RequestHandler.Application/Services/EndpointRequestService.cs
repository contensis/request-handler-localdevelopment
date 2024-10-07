using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Net.Http.Headers;
using Zengenti.Contensis.RequestHandler.Application.Middleware;
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

    private readonly IHttpClientFactory _clientFactory;

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
        var isHealthCheck = headers != null &&
                            headers.ContainsKey(Constants.Headers.HealthCheck) &&
                            headers[Constants.Headers.HealthCheck].ContainsCaseInsensitive("true");

        if (routeInfo.IsIisFallback && isHealthCheck)
        {
            headers!.Add(
                HeaderNames.ContentType,
                new[]
                {
                    "application/json"
                });
            var responseContent = new
            {
                msg = "No route matched so returning a success as the Classic backend will be used"
            }.ToJsonWithLowercasePropertyNames();
            return new EndpointResponse(responseContent, httpMethod, headers, (int)HttpStatusCode.OK);
        }

        RecursionChecker.Check(currentDepth, routeInfo);

        var measurer = new PageletPerformanceMeasurer(_requestContext.TraceEnabled, autoStart: true);

        _logger.LogDebug("Making request to {Uri}", routeInfo.Uri);

        var isStreamingRequestMessage = IsStreamingRequestMessage(httpMethod, routeInfo);

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

            using var responseMessage = await httpClient.SendAsync(
                targetRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            measurer.EndOfRequest();

            var endpointResponse = await GetContent(
                routeInfo,
                responseMessage,
                currentDepth,
                cancellationToken,
                measurer,
                targetRequestMessage.RequestUri,
                targetRequestMessage.Method,
                targetRequestMessage.Headers);

            if (!endpointResponse.IsErrorStatusCode())
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var curlString = ErrorResources.CreateCurlCallString(routeInfo);

                    _logger.LogDebug(
                        "Invoking endpoint {Uri} was successful with status code {StatusCode}. The equivalent curl command is: {CurlString}",
                        routeInfo.Uri,
                        endpointResponse.StatusCode,
                        curlString);
                }

                return endpointResponse;
            }

            var absoluteUri = routeInfo.Uri.AbsoluteUri;

            if (absoluteUri.EndsWithCaseInsensitive("/favicon.ico"))
            {
                return endpointResponse;
            }

            // log the response content if the status code is 5xx
            var logLevel = LogLevel.Information;
            var responseContent = "";
            if (endpointResponse.StatusCode >= 500)
            {
                logLevel = LogLevel.Warning;

                if (!string.IsNullOrWhiteSpace(endpointResponse.StringContent))
                {
                    responseContent = endpointResponse.StringContent;
                }
                else
                {
                    var endpointResponseStream = endpointResponse.ToStream(true);
                    if (endpointResponseStream is { Length: > 0 } and MemoryStream stream)
                    {
                        using var reader = new StreamReader(stream, leaveOpen: true);
                        responseContent = await reader.ReadToEndAsync();
                    }
                }
            }

            var state = new
            {
                requestContext = JsonSerializer.Serialize(_requestContext),
                routeInfo = JsonSerializer.Serialize(routeInfo),
                responseHeaders = JsonSerializer.Serialize(endpointResponse.Headers),
                responseContent
            };

            using (_logger.BeginScope(state))
            {
                var curlString = ErrorResources.CreateCurlCallString(routeInfo);

                _logger.Log(
                    logLevel,
                    "Invoking endpoint {AbsoluteUri} was not successful: {StatusCode}. The equivalent curl command is: {CurlString}",
                    absoluteUri,
                    endpointResponse.StatusCode,
                    curlString);
            }

            return endpointResponse;
        }
        catch (Exception e)
        {
            var endpointResponse = new EndpointResponse(
                string.Empty,
                httpMethod,
                new Dictionary<string, IEnumerable<string>>(),
                500);

            // If the request was cancelled, return the error response without logging
            if (e is TaskCanceledException && cancellationToken.IsCancellationRequested)
            {
                return endpointResponse;
            }

            using (_logger.BeginScope(
                new
                {
                    requestContext = JsonSerializer.Serialize(_requestContext),
                    routeInfo = JsonSerializer.Serialize(routeInfo),
                    cancelledRequest = cancellationToken.IsCancellationRequested.ToString()
                }))
            {
                var routeInfoType = "";
                if (routeInfo.IsIisFallback)
                {
                    routeInfoType = "IIS falback ";
                }
                else if (routeInfo.BlockVersionInfo != null)
                {
                    routeInfoType = "block ";
                }

                var curlString = ErrorResources.CreateCurlCallString(routeInfo);

                _logger.LogError(
                    e,
                    "Failed to invoke {RouteInfoType} endpoint {Uri} with http method {Method}. The equivalent curl command is: {CurlString}",
                    routeInfoType,
                    routeInfo.Uri,
                    httpMethod.Method,
                    curlString);
            }

            return endpointResponse;
        }
    }

    private bool IsStreamingRequestMessage(HttpMethod httpMethod, RouteInfo routeInfo)
    {
        if (routeInfo.BlockVersionInfo != null &&
            (httpMethod == HttpMethod.Post ||
             httpMethod == HttpMethod.Put ||
             httpMethod == HttpMethod.Patch))
        {
            return true;
        }

        if (routeInfo.IsIisFallback && httpMethod == HttpMethod.Get && routeInfo.Uri.AbsolutePath.EndsWithCaseInsensitive(".mp4"))
        {
            return true;
        }

        return false;
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
                requestMethod,
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
            requestMethod,
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

    private void AddHeaders(
        RouteInfo routeInfo,
        HttpRequestMessage requestMessage,
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

            if (!DisallowedRequestHeaderMappings.ContainsCaseInsensitive(key))
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

        if (routeInfo.IsIisFallback && !string.IsNullOrWhiteSpace(_requestContext.IisHostname))
        {
            // Override host header with IIS fallback host
            requestMessage.Headers.Host = _requestContext.IisHostname;
        }
        else
        {
            var host = routeInfo.Headers.GetFirstValueIfExists(Constants.Headers.Host);

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