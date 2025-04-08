using System.Diagnostics.CodeAnalysis;
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

public class EndpointRequestService(
    IHttpClientFactory clientFactory,
    IResponseResolverService responseResolverService,
    IRequestContext requestContext,
    ICacheKeyService cacheKeyService,
    RequestHeaderMappingService requestHeaderMappingService,
    ILogger<EndpointRequestService> logger)
    : IEndpointRequestService
{
    public static readonly string[] SensitiveHeadersToBeRemovedFromCurl =
    [
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
    ];

    private static readonly string[] UserAgentBots =
    [
        "bingbot",
        "(bot@ecotrek.tech)",
        "ChatGPT-User",
        "DataForSeoBot",
        "Googlebot",
        "InsytfulBot",
        "Mastodon",
        "PetalBot",
        "Qwantbot",
        "Sidetrade indexer bot",
        "Snap URL Preview Service",
        "YandexBot"
    ];

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

        if (routeInfo.RouteType == RouteType.IisFallback && isHealthCheck)
        {
            headers!.Add(
                HeaderNames.ContentType,
                [
                    "application/json"
                ]);
            var responseContent = new
            {
                msg = "No route matched so returning a success as the Classic backend will be used"
            }.ToJsonWithLowercasePropertyNames();
            return new EndpointResponse(responseContent, httpMethod, headers, (int)HttpStatusCode.OK);
        }

        RecursionChecker.Check(currentDepth, routeInfo);

        var measurer = new PageletPerformanceMeasurer(requestContext.TraceEnabled, autoStart: true);

        logger.LogDebug("Making request to {Uri}", routeInfo.Uri);

        var isStreamingRequest = IsStreamingRequestMessage(httpMethod, routeInfo);

        using var targetRequestMessage =
            await CreateRequestMessage(httpMethod, content, headers, routeInfo, isStreamingRequest);

        try
        {
            var httpClient = clientFactory.CreateClient("no-auto-redirect");
            var requestTimeoutInMinutes = 6 * 10;
            if (isStreamingRequest && httpClient.Timeout.TotalMinutes < requestTimeoutInMinutes)
            {
                httpClient.Timeout = new TimeSpan(0, requestTimeoutInMinutes, 0);
            }

            var responseMessage = await httpClient.SendAsync(
                targetRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            var shouldParseContent = false;
            try
            {
                measurer.EndOfRequest();

                shouldParseContent = ShouldParseContent(routeInfo, responseMessage);
                var endpointResponse = await GetContent(
                    routeInfo,
                    responseMessage,
                    currentDepth,
                    cancellationToken,
                    measurer,
                    targetRequestMessage.RequestUri,
                    targetRequestMessage.Method,
                    targetRequestMessage.Headers,
                    shouldParseContent);

                if (!endpointResponse.IsErrorStatusCode())
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        var curlString = ErrorResources.CreateCurlCallString(routeInfo);

                        logger.LogDebug(
                            "Invoking endpoint {Uri} was successful with status code {StatusCode}. The equivalent curl command in PowerShell is: {CurlString}",
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

                // log the response content if the status code is 400 or 5xx
                var logLevel = LogLevel.Information;
                var responseContent = "";
                if (endpointResponse.StatusCode != 404 && endpointResponse.StatusCode is >= 400 and < 600)
                {
                    var isDevCmsRequest = IsDevCmsRequest(headers);

                    var isBotRequest = IsBotRequest(headers);

                    logLevel = isDevCmsRequest || isBotRequest ? LogLevel.Information : LogLevel.Warning;

                    if (!string.IsNullOrWhiteSpace(endpointResponse.StringContent))
                    {
                        responseContent = endpointResponse.StringContent;
                    }
                    else
                    {
                        var endpointResponseStream = endpointResponse.ToStream(true);
                        if (endpointResponseStream is MemoryStream { CanSeek: true, Length: > 0 } stream)
                        {
                            using var reader = new StreamReader(stream, leaveOpen: true);
                            responseContent = await reader.ReadToEndAsync(cancellationToken);
                        }
                    }
                }

                var state = new
                {
                    requestContext = JsonSerializer.Serialize(requestContext),
                    routeInfo = JsonSerializer.Serialize(routeInfo),
                    responseHeaders = JsonSerializer.Serialize(endpointResponse.Headers),
                    responseContent
                };

                using (logger.BeginScope(state))
                {
                    var curlString = ErrorResources.CreateCurlCallString(routeInfo);

                    logger.Log(
                        logLevel,
                        "Invoking endpoint {AbsoluteUri} was not successful: {StatusCode}. The equivalent curl command in PowerShell is: {CurlString}",
                        absoluteUri,
                        endpointResponse.StatusCode,
                        curlString);

                    routeInfo.DebugData.EndpointError = $"{endpointResponse.StatusCode}";
                    routeInfo.DebugData.EndpointErrorCurl = curlString.Replace(" `\n ", "");
                }

                return endpointResponse;
            }
            finally
            {
                if (shouldParseContent)
                {
                    responseMessage.Dispose();
                }
            }
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

            using (logger.BeginScope(
                new
                {
                    requestContext = JsonSerializer.Serialize(requestContext),
                    routeInfo = JsonSerializer.Serialize(routeInfo),
                    cancelledRequest = cancellationToken.IsCancellationRequested.ToString()
                }))
            {
                var routeInfoType = "";
                if (routeInfo.RouteType == RouteType.IisFallback)
                {
                    routeInfoType = "IIS falback ";
                }
                else if (routeInfo.BlockVersionInfo != null)
                {
                    routeInfoType = "block ";
                }

                var curlString = ErrorResources.CreateCurlCallString(routeInfo);

                var logLevel = IsBotRequest(headers) ? LogLevel.Information : LogLevel.Error;

                logger.Log(
                    logLevel,
                    e,
                    "Failed to invoke {RouteInfoType} endpoint {Uri} with http method {Method}. The equivalent curl command in PowerShell is: {CurlString}",
                    routeInfoType,
                    routeInfo.Uri,
                    httpMethod.Method,
                    curlString);

                routeInfo.DebugData.EndpointError =
                    $"Failed to invoke {routeInfoType} endpoint with http method {httpMethod.Method}";
                routeInfo.DebugData.EndpointErrorCurl = curlString.Replace(" `\n ", "");
                ;
            }

            return endpointResponse;
        }
    }

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    private static bool IsBotRequest(Dictionary<string, IEnumerable<string>>? headers)
    {
        if (headers == null || headers.Count == 0)
        {
            return false;
        }

        var isBotRequest = false;
        if (headers.TryGetValue(HeaderNames.UserAgent, out var userAgentValues) &&
            userAgentValues.Any())
        {
            var userAgentValue = userAgentValues.First();
            isBotRequest = userAgentValue.Contains("bot", StringComparison.InvariantCultureIgnoreCase) &&
                           UserAgentBots.Any(
                               bot => userAgentValue.Contains(
                                   bot,
                                   StringComparison.InvariantCultureIgnoreCase));
        }

        if (!isBotRequest &&
            headers.TryGetValue(HeaderNames.From, out var fromValues) &&
            fromValues.Any())
        {
            var fromValue = fromValues.First();
            isBotRequest = fromValue.Contains("bot", StringComparison.InvariantCultureIgnoreCase) &&
                           UserAgentBots.Any(
                               bot => fromValue.Contains(
                                   bot,
                                   StringComparison.InvariantCultureIgnoreCase));
        }

        return isBotRequest;
    }

    private static bool IsDevCmsRequest(Dictionary<string, IEnumerable<string>>? headers)
    {
        var isDevCms = headers != null &&
                       headers.ContainsKey(Constants.Headers.Alias) &&
                       headers[Constants.Headers.Alias].Any() &&
                       (headers[Constants.Headers.Alias].First().EndsWithCaseInsensitive("-dev") ||
                        headers[Constants.Headers.Alias].First().EndsWithCaseInsensitive("-deva") ||
                        headers[Constants.Headers.Alias].First().EndsWithCaseInsensitive("-devb"));
        return isDevCms;
    }

    private bool IsStreamingRequestMessage(HttpMethod httpMethod, RouteInfo routeInfo)
    {
        return routeInfo.BlockVersionInfo != null &&
               (httpMethod == HttpMethod.Post ||
                httpMethod == HttpMethod.Put ||
                httpMethod == HttpMethod.Patch);
    }

    private async Task<EndpointResponse> GetContent(
        RouteInfo routeInfo,
        HttpResponseMessage responseMessage,
        int currentDepth,
        CancellationToken ct,
        PageletPerformanceMeasurer measurer,
        Uri? requestUri,
        HttpMethod requestMethod,
        HttpRequestHeaders requestMessageHeaders,
        bool doParseContent)
    {
        var responseHeaders = GetResponseHeaders(responseMessage);

        cacheKeyService.Add(responseHeaders);

        if (doParseContent)
        {
            var resolvedContent =
                await responseResolverService.Resolve(responseMessage, routeInfo, currentDepth, ct);

            measurer.EndOfParsing();
            measurer.End();

            return new EndpointResponse(
                resolvedContent,
                requestMethod,
                responseHeaders,
                (int)responseMessage.StatusCode,
                requestContext.TraceEnabled
                    ? new PageletPerformanceData(measurer, requestUri, requestMethod, requestMessageHeaders)
                    : null);
        }

        var responseStream = await responseMessage.Content.ReadAsStreamAsync(ct);

        measurer.EndOfParsing();
        measurer.End();

        return new EndpointResponse(
            responseStream,
            requestMethod,
            responseHeaders,
            (int)responseMessage.StatusCode,
            requestContext.TraceEnabled
                ? new PageletPerformanceData(measurer, requestUri, requestMethod, requestMessageHeaders)
                : null);
    }

    private bool ShouldParseContent(RouteInfo routeInfo, HttpResponseMessage responseMessage)
    {
        return responseMessage.IsResponseResolvable() &&
               (routeInfo.ProxyInfo?.ParseContent == true || routeInfo.BlockVersionInfo?.BlockVersionId != null);
    }

    private async Task<HttpRequestMessage> CreateRequestMessage(
        HttpMethod httpMethod,
        Stream? content,
        Dictionary<string, IEnumerable<string>>? headers,
        RouteInfo routeInfo,
        bool isStreamingRequest)
    {
        var requestMessage = new HttpRequestMessage
        {
            RequestUri = routeInfo.Uri,
            Method = httpMethod
        };

        if (content != null)
        {
            if (isStreamingRequest)
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

        requestHeaderMappingService.MapHeaders(requestMessage, headers);

        // TODO Characterise and move to RequestHeaderMappingService.MapHeaders()
        if (headers.ContainsKey("Content-Type") &&
            MediaTypeHeaderValue.TryParse(string.Join(",", headers["Content-Type"]), out var parsedValue))
        {
            if (requestMessage.Content is not null)
            {
                requestMessage.Content.Headers.ContentType = parsedValue;
            }
        }

        if (routeInfo.RouteType == RouteType.IisFallback && !string.IsNullOrWhiteSpace(requestContext.IisHostname))
        {
            // Override host header with IIS fallback host
            requestMessage.Headers.Host = requestContext.IisHostname;
        }
        else
        {
            var host = routeInfo.Headers.GetFirstValueIfExists(Constants.Headers.Host);

            requestMessage.Headers.Host = host ?? routeInfo.Uri.Host;
        }
        // TODO End
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