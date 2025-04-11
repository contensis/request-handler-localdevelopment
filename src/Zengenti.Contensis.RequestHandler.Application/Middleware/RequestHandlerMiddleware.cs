using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Web;
using Microsoft.AspNetCore.Http.Extensions;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Middleware;

public class RequestHandlerMiddleware(
    RequestDelegate nextMiddleware,
    IRequestContext requestContext,
    IRouteService routeService,
    IRouteInfoFactory routeInfoFactory,
    ICacheKeyService cacheKeyService,
    IEndpointRequestService endpointRequestService,
    IGlobalApi globalApi,
    CallContextService callContextService,
    ILogger<RequestHandlerMiddleware> logger)
{
    private static readonly ActivitySource ActivitySource = new("Zengenti.Contensis.RequestHandler.Middleware");

    private static readonly string[] ExcludedPaths =
    [
        "/api/preview-toolbar/blocks",
        "/pingz",
        "/healthz",
        "/infoz",
        "/livez"
    ];

    public IEndpointRequestService RequestService { get; } = endpointRequestService;

    internal IRouteService RouteService { get; } = routeService;

    public async Task Invoke(HttpContext context)
    {
        using var activity = ActivitySource.StartActivity();
        SetContextValues(context.Request, activity);

        try
        {
            var response = await GetRequestServiceResponse(context);
            if (response != null)
            {
                await HandleResponse(context, response);
                return;
            }

            await nextMiddleware(context);
        }
        catch (Exception e)
        {
            if (e is AggregateException aggregateException)
            {
                var isExceptionHandled =
                    await ExceptionHandler.HandlePageletExceptions(context, aggregateException);

                if (isExceptionHandled)
                {
                    return;
                }
            }

            using (logger.BeginScope(
                new
                {
                    alias = CallContext.Current[Constants.Headers.Alias] ?? "",
                    projectUuid = CallContext.Current[Constants.Headers.ProjectUuid] ?? "",
                    projectApiId = CallContext.Current[Constants.Headers.ProjectApiId] ?? "",
                    blockVersionConfig = CallContext.Current[Constants.Headers.BlockConfig] ?? "",
                    proxyConfig = CallContext.Current[Constants.Headers.ProxyConfig] ?? "",
                    rendererConfig = CallContext.Current[Constants.Headers.RendererConfig] ?? "",
                    nodeConfig = CallContext.Current[Constants.Headers.NodeVersionStatus] ?? ""
                }))
            {
                if (e.Data.Contains(Constants.Exceptions.DataKeyForOriginalMessage))
                {
                    logger.LogError(
                        e,
                        "Unhandled error caught in middleware with exception message {Message} and request url {Url}. Initial message: {InitialMessage}",
                        e.Message,
                        context.Request.GetDisplayUrl(),
                        e.Data[Constants.Exceptions.DataKeyForOriginalMessage]);
                }
                else
                {
                    logger.LogError(
                        e,
                        "Unhandled error caught in middleware with exception message {Message} and request url {Url}",
                        e.Message,
                        context.Request.GetDisplayUrl());
                }
            }

            throw;
        }
        finally
        {
            CallContext.Current.Clear();
        }
    }

    private Dictionary<string, string> GetConfigCookieCollection(HttpRequest request)
    {
        var activeVersionConfigDictionary = new Dictionary<string, string>();

        foreach (var query in request.Query)
        {
            var key = query.Key.ToLowerInvariant();
            if (query.Value.Count > 0 && callContextService.IsVersionConfigKey(key))
            {
                var value = query.Value.First();
                if (value != null)
                {
                    activeVersionConfigDictionary[key] = value;
                }
            }
        }

        return activeVersionConfigDictionary;
    }

    private void SetContextValues(HttpRequest request, Activity? activity)
    {
        callContextService.SetCallContextData(request);
        activity?.SetTag("alias", CallContext.Current[Constants.Headers.Alias]);
        activity?.SetTag("projectUuid", CallContext.Current[Constants.Headers.ProjectUuid]);
        activity?.SetTag("projectApiId", CallContext.Current[Constants.Headers.ProjectApiId]);
        activity?.SetTag("blockVersionConfig", CallContext.Current[Constants.Headers.BlockConfig]);
        activity?.SetTag("proxyConfig", CallContext.Current[Constants.Headers.ProxyConfig]);
        activity?.SetTag("rendererConfig", CallContext.Current[Constants.Headers.RendererConfig]);
        activity?.SetTag("nodeConfig", CallContext.Current[Constants.Headers.NodeVersionStatus]);
    }

    private RouteInfo CreateIisFallbackRouteInfo(
        HttpContext context,
        Headers headers,
        RouteInfo? originalRouteInfo)
    {
        if (!string.IsNullOrWhiteSpace(requestContext.IisHostname) &&
            !string.IsNullOrWhiteSpace(requestContext.LoadBalancerVip))
        {
            return routeInfoFactory.CreateForIisFallback(
                context.Request.GetOriginUri(),
                headers,
                originalRouteInfo);
        }

        // TODO: the Uri property is expected to be null - we need to change it to be nullable
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        return routeInfoFactory.CreateNotFoundRoute(headers);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    private string GetPerformanceInfo(
        Stopwatch routeInfoTimer,
        Stopwatch responseTimer,
        Stopwatch fallbackTimer,
        string mainRouteInfoMetrics)
    {
        return string.Format(
            "routeInfoFetch: {0} ms ({3}), generalRequestFetch: {1} ms, fallbackRequest {2} ms",
            routeInfoTimer.ElapsedMilliseconds,
            responseTimer.ElapsedMilliseconds,
            fallbackTimer.ElapsedMilliseconds,
            mainRouteInfoMetrics);
    }

    private bool IsValidVersionStatus(string? entryVersionStatus)
    {
        switch (entryVersionStatus)
        {
            case null:
            case "":
                return false;
            case "published":
            case "latest":
                return true;
            default:
                return false;
        }
    }

    private void ConfigureRequestHeaders(HttpContext context, Headers headers)
    {
        //Fix after other cookie work.
        var serverType = new ServerTypeResolver().GetServerType();
        if (serverType != ServerType.Live &&
            context.Request.Query.ContainsKey(Constants.QueryStrings.EntryVersionStatus))
        {
            var entryVersionStatus = context.Request.Query[Constants.QueryStrings.EntryVersionStatus];
            if (IsValidVersionStatus(entryVersionStatus))
            {
                headers[Constants.Headers.EntryVersionStatus] = entryVersionStatus;
            }
        }
        else
        {
            if (!context.Request.Cookies.ContainsKey(Constants.Headers.EntryVersionStatus)) return;
            var configuredEntryVersionStatus = context.Request.Cookies[Constants.Headers.EntryVersionStatus];
            if (IsValidVersionStatus(configuredEntryVersionStatus))
            {
                headers[Constants.Headers.EntryVersionStatus] = configuredEntryVersionStatus;
            }
        }
    }

    private async Task<EndpointResponse?> GetRequestServiceResponse(HttpContext context)
    {
        if (ExcludedPaths.ContainsCaseInsensitive(context.Request.Path))
        {
            return null;
        }

        var headers = new Headers(context.Request.Headers);

        var redirectResponse = GetRedirectResponseIfRequired(context, headers);
        if (redirectResponse != null)
        {
            return redirectResponse;
        }

        ConfigureRequestHeaders(context, headers);

        var routeInfoTimer = new Stopwatch();
        routeInfoTimer.Start();

        var routeInfo = await RouteService.GetRouteForRequest(context.Request, headers);
        var initialRouteInfo = routeInfo;

        var mainRouteInfoMetrics = "";
        if (routeInfo.RouteType == RouteType.NotFound)
        {
            // No route info returned (No node/block/renderer/proxy found) so try and fallback to the IIS site if specified
            routeInfo = CreateIisFallbackRouteInfo(context, headers, routeInfo);
            if (routeInfo.RouteType == RouteType.NotFound)
            {
                return null;
            }
        }
        else
        {
            mainRouteInfoMetrics = routeInfo.Metrics.ToString();
        }

        routeInfoTimer.Stop();

        var responseTimer = new Stopwatch();
        responseTimer.Start();
        EndpointResponse response = null!;

        var hasValidResponse = false;
        // if we have a proxy route for a partial matched path make an IIS fallback request first
        var isPartialMatchProxy = routeInfo is { RouteType: RouteType.Proxy, ProxyInfo.IsPartialMatchPath: true };
        if (isPartialMatchProxy)
        {
            // The headers need to be copied as they will be modified
            var proxyIisFalbackHeaders = new Headers(headers);
            var proxyIisFallbackRouteInfo = CreateIisFallbackRouteInfo(context, proxyIisFalbackHeaders, routeInfo);
            if (proxyIisFallbackRouteInfo.RouteType != RouteType.NotFound)
            {
                response = await RequestService.Invoke(
                    context.Request.GetHttpMethod(),
                    context.Request.Body,
                    proxyIisFalbackHeaders,
                    proxyIisFallbackRouteInfo,
                    0,
                    context.RequestAborted);

                SetStatusCode404IfEmptyIisResponse(response);
                if (response.IsErrorStatusCode())
                {
                    routeInfo.DebugData.AppConfiguration = proxyIisFallbackRouteInfo.DebugData.AppConfiguration;
                    routeInfo.DebugData.InitialDebugData = proxyIisFallbackRouteInfo.DebugData;
                }
                else
                {
                    proxyIisFallbackRouteInfo.DebugData.InitialDebugData = null;
                    proxyIisFallbackRouteInfo.DebugData.AdditionalDebugData = routeInfo.DebugData;

                    headers = proxyIisFalbackHeaders;
                    routeInfo = proxyIisFallbackRouteInfo;
                    hasValidResponse = true;
                }
            }
        }

        if (!hasValidResponse)
        {
            response = await RequestService.Invoke(
                context.Request.GetHttpMethod(),
                context.Request.Body,
                headers,
                routeInfo,
                0,
                context.RequestAborted);
        }

        responseTimer.Stop();

        var fallbackTimer = new Stopwatch();
        fallbackTimer.Start();
        if (response.StatusCode == (int)HttpStatusCode.NotFound &&
            routeInfo.RouteType != RouteType.IisFallback &&
            !isPartialMatchProxy)
        {
            // Block/proxy request returned 404 so try and fallback to the IIS site if specified
            var fallbackRouteInfo = CreateIisFallbackRouteInfo(context, headers, routeInfo);
            if (fallbackRouteInfo.RouteType != RouteType.NotFound)
            {
                routeInfo = fallbackRouteInfo;
                response = await RequestService.Invoke(
                    context.Request.GetHttpMethod(),
                    context.Request.Body,
                    headers,
                    routeInfo,
                    0,
                    context.RequestAborted);
            }
        }

        response = await GenerateFriendlyErrorResponse(context, routeInfo, initialRouteInfo, response);

        FixResponseHeadersAndStatusCodes(routeInfo, response);

        AddCacheHeadersFor404Errors(response, context);

        fallbackTimer.Stop();
        var performanceLog = GetPerformanceInfo(routeInfoTimer, responseTimer, fallbackTimer, mainRouteInfoMetrics);

        EnsureMetricsAndDebugDataHeaders(context, response, routeInfo, performanceLog);

        UpdateLocationResponseHeader(context, response, routeInfo);

        return response;
    }

    private void EnsureMetricsAndDebugDataHeaders(
        HttpContext context,
        EndpointResponse response,
        RouteInfo routeInfo,
        string performanceLog)
    {
        response.Headers["request-handler-metrics"] =
            new List<string>
            {
                performanceLog
            };

        if (context.Request.Headers[Constants.Headers.Debug] == "true" ||
            context.Request.Headers[Constants.Headers.AltDebug] == "true")
        {
            response.Headers["request-handler-debug-data"] = new List<string>
            {
                routeInfo.DebugData.ToString()
            };

            if (routeInfo.DebugData.InitialDebugData != null)
            {
                response.Headers["request-handler-initial-debug-data"] = new List<string>
                {
                    routeInfo.DebugData.InitialDebugData.ToString()
                };
            }

            if (routeInfo.DebugData.AdditionalDebugData != null)
            {
                response.Headers["request-handler-additional-debug-data"] = new List<string>
                {
                    routeInfo.DebugData.AdditionalDebugData.ToString()
                };
            }
        }
    }

    private EndpointResponse? GetRedirectResponseIfRequired(HttpContext context, Headers headers)
    {
        var queryStringBlockConfigData = GetConfigCookieCollection(context.Request);

        if (queryStringBlockConfigData.Count == 0)
        {
            return null;
        }

        var blockConfigCookie = string.Empty;
        var rendererConfigCookie = string.Empty;
        var proxyConfigCookie = string.Empty;
        foreach (var (key, value) in queryStringBlockConfigData)
        {
            if (key.StartsWith("block-"))
            {
                if (blockConfigCookie.Length > 0)
                {
                    blockConfigCookie += "&";
                }

                blockConfigCookie += $"{key}={value}";
            }

            if (key.StartsWith("proxy-"))
            {
                if (proxyConfigCookie.Length > 0)
                {
                    proxyConfigCookie += "&";
                }

                proxyConfigCookie += $"{key}={value}";
            }

            if (key.StartsWith("renderer-"))
            {
                if (rendererConfigCookie.Length > 0)
                {
                    rendererConfigCookie += "&";
                }

                rendererConfigCookie += $"{key}={value}";
            }
        }

        if (blockConfigCookie.Length == 0 && rendererConfigCookie.Length == 0 && proxyConfigCookie.Length == 0)
        {
            return null;
        }

        var redirectResponse = new EndpointResponse("", context.Request.GetHttpMethod(), headers, 301);

        var newQueryString = HttpUtility.ParseQueryString(context.Request.QueryString.Value ?? "");

        var keysToRemove = new List<string>();
        foreach (string queryKey in newQueryString.Keys)
        {
            if (callContextService.IsVersionConfigKey(queryKey))
            {
                keysToRemove.Add(queryKey);
            }
        }

        foreach (var keyToRemove in keysToRemove)
        {
            newQueryString.Remove(keyToRemove);
        }

        var updatedUri = context.Request.Path.Value!;
        if (newQueryString.Count > 0)
        {
            updatedUri += $"?{newQueryString}";
        }

        redirectResponse.Headers["location"] = new List<string>
        {
            updatedUri
        };

        if (blockConfigCookie.Length > 0)
        {
            context.Response.Cookies.Append(Constants.Headers.BlockConfig, blockConfigCookie);
        }

        if (rendererConfigCookie.Length > 0)
        {
            context.Response.Cookies.Append(Constants.Headers.RendererConfig, rendererConfigCookie);
        }

        if (proxyConfigCookie.Length > 0)
        {
            context.Response.Cookies.Append(Constants.Headers.ProxyConfig, proxyConfigCookie);
        }

        // TODO: may want to set this to 0 later.
        // redirectResponse.Headers["Surrogate-Control"] = new List<string>() { "max-age=30" };
        return redirectResponse;
    }

    private async Task HandleResponse(HttpContext context, EndpointResponse response)
    {
        try
        {
            context.Response.StatusCode = response.StatusCode;

            // Headers
            SetResponseHeaders(context, response.Headers);

            // The content may have changed due to resolving pagelets and re-writing static paths, so get the actual length.
            var responseContent = response.ToStream(true);
            if (responseContent is MemoryStream && context.Response.ContentLength != responseContent.Length)
            {
                context.Response.ContentLength = responseContent.Length;
            }

            if (responseContent != null)
            {
                await responseContent.CopyToAsync(context.Response.Body);
            }
        }
        catch (OperationCanceledException oce)
        {
            logger.LogWarning(
                oce,
                "Response stream closed by client",
                context.Request);
        }
        catch (IOException ioe)
        {
            logger.LogWarning(
                ioe,
                "Possible client disconnection",
                context.Request);
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "An unexpected error occured",
                context.Request);
        }
    }

    private async Task<EndpointResponse> GenerateFriendlyErrorResponse(
        HttpContext context,
        RouteInfo routeInfo,
        RouteInfo? initialRouteInfo,
        EndpointResponse response)
    {
        if (routeInfo.Headers.SiteType.EqualsCaseInsensitive("live"))
        {
            return response;
        }

        if (routeInfo.RouteType == RouteType.IisFallback)
        {
            if (initialRouteInfo == null)
            {
                return response;
            }

            if (response.StatusCode == (int)HttpStatusCode.NotFound)
            {
                var responseHtml =
                    ErrorResources.GetIisFallbackMessage(
                        response.StatusCode,
                        routeInfo,
                        initialRouteInfo.NodePath ?? "");

                return await GetFriendlyErrorResponse(context, routeInfo.Uri.Query, response, responseHtml);
            }
        }

        if (response.StatusCode == (int)HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == (int)HttpStatusCode.NotFound ||
            response.StatusCode == (int)HttpStatusCode.InternalServerError)
        {
            var responseHtml = "";

            if (initialRouteInfo?.BlockVersionInfo != null)
            {
                responseHtml = ErrorResources.GetMessage(response.StatusCode, initialRouteInfo);
                if (string.IsNullOrEmpty(responseHtml))
                {
                    responseHtml =
                        $"<html><body><h1>{response.StatusCode} </h1> cannot retrieve custom message from assembly.</body></html>";
                }
            }
            else
            {
                if (response.StatusCode == (int)HttpStatusCode.ServiceUnavailable)
                {
                    responseHtml =
                        "<html><body><h1>503 Service Unavailable</h1> No server is available to handle this request.</body></html>";
                }

                if (response.StatusCode == (int)HttpStatusCode.NotFound)
                {
                    responseHtml = "<html><body><h1>404 Not Found</h1> No page can be found.</body></html>";
                }

                if (response.StatusCode == (int)HttpStatusCode.InternalServerError)
                {
                    responseHtml = "<html><body><h1>500 InternalServerError</h1> An error has occured.</body></html>";
                }
            }

            return await GetFriendlyErrorResponse(context, routeInfo.Uri.Query, response, responseHtml);
        }

        return response;
    }

    private async Task<EndpointResponse> GetFriendlyErrorResponse(
        HttpContext context,
        string query,
        EndpointResponse response,
        string responseHtml)
    {
        if (context.Request.Headers.TryGetValue(
                Constants.Headers.ProjectApiId,
                out var projectApiId) &&
            context.Request.Headers.TryGetValue(
                Constants.Headers.Alias,
                out var alias) &&
            context.Request.Headers.TryGetValue(
                Constants.Headers.EntryVersionStatus,
                out var entryVersionStatus))
        {
            var isContensisSingleSignOn = await globalApi.IsContensisSingleSignOn();
            HtmlResponseResolver.SetPreviewToolbar(
                ref responseHtml,
                alias[0]!,
                projectApiId[0]!,
                entryVersionStatus[0]!,
                query,
                isContensisSingleSignOn);
        }

        response = new EndpointResponse(
            responseHtml,
            response.HttpMethod,
            response.Headers,
            response.StatusCode,
            response.PageletPerformanceData)
        {
            Headers =
            {
                // This ensures varnish will not replace our 503 with its own.
                ["expose-raw-errors"] = new List<string>
                {
                    "True"
                },
                ["content-type"] = new List<string>
                {
                    "text/html; charset=utf-8"
                }
            }
        };

        return response;
    }

    private static void FixResponseHeadersAndStatusCodes(RouteInfo routeInfo, EndpointResponse response)
    {
        response.Headers[Constants.Headers.IsIisFallback] =
        [
            (routeInfo.RouteType == RouteType.IisFallback).ToString().ToLower()
        ];

        if (routeInfo.RouteType != RouteType.IisFallback)
        {
            if (routeInfo is { ProxyInfo: not null, BlockVersionInfo: null })
            {
                // TODO: when we introduce cache settings on proxies this is where it needs to be implemented
                // we need to check if it is an error or not
                if (routeInfo.ProxyInfo.ProxyId.Equals(Guid.Parse("8f2cc5be-b5dd-4e4b-b6fa-92b7fc6440e0")))
                {
                    response.Headers[Constants.Headers.SurrogateControl] =
                    [
                        "max-age=60"
                    ];

                    return;
                }
            }
        }

        if (routeInfo.RouteType == RouteType.IisFallback)
        {
            if (SetStatusCode404IfEmptyIisResponse(response)) return;

            // if not a 404 error and is an error code of 400 or greater than 404 then we need to set the surrogate control to 5 seconds
            if (response.StatusCode is 400 or > 404)
            {
                response.Headers[Constants.Headers.SurrogateControl] =
                [
                    "max-age=5"
                ];
            }
            else if (response.StatusCode is > 400 and < 404)
            {
                response.Headers[Constants.Headers.SurrogateControl] =
                [
                    "max-age=0"
                ];
            }
        }

        if (!response.IsErrorStatusCode() && routeInfo.Headers.OrigHost.StartsWithCaseInsensitive("preview"))
        {
            response.Headers[Constants.Headers.SurrogateControl] =
            [
                "max-age=0"
            ];
        }
    }

    private static bool SetStatusCode404IfEmptyIisResponse(EndpointResponse response)
    {
        // Unfortunately IIS returns an empty bodied 200 rather than a 404 when
        // hitting the root or a directory of a site, where there is no default page or
        // no extension in the path requested.
        var responseStream = response.ToStream();
        if (response.StatusCode == (int)HttpStatusCode.OK &&
            response.HttpMethod != HttpMethod.Options &&
            responseStream is { CanSeek: true, Length: 0 }
        )
        {
            response.StatusCode = 404;
            return true;
        }

        return false;
    }

    private static void AddCacheHeadersFor404Errors(EndpointResponse response, HttpContext context)
    {
        if (response.StatusCode != 404 || context.Response.Headers.ContainsKey("Surrogate-Control")) return;

        if (context.Request.Path == "/")
        {
            response.Headers[Constants.Headers.SurrogateControl] =
            [
                "max-age=30"
            ];
            return;
        }

        //This is to prevent malicious requests bombarding the backend services.
        response.Headers[Constants.Headers.SurrogateControl] =
        [
            "max-age=5"
        ];
    }

    private static bool PotentiallyHasLocationHeader(EndpointResponse response)
    {
        if (response.StatusCode == 301 ||
            response.StatusCode == 302 ||
            response.StatusCode == 307 ||
            response.StatusCode == 308)
        {
            return true;
        }

        return false;
    }

    private static void RestoreOrRemoveInjectedParam(HttpContext context, NameValueCollection queryString, string param)
    {
        var paramOrigValue = context.Request.Query[param];
        if (paramOrigValue.Count == 0)
        {
            //Remove the param we added, unless they specifically passed it in the origin query
            queryString.Remove(param);
        }
        else
        {
            queryString.Add(param, paramOrigValue.ToString());
        }
    }

    private static void UpdateLocationResponseHeader(
        HttpContext context,
        EndpointResponse response,
        RouteInfo routeInfo)
    {
        if (!PotentiallyHasLocationHeader(response))
        {
            return;
        }

        if (!response.Headers.TryGetValue("Location", out var location))
        {
            // Unfortunately IIS returns an empty bodied 301 sometimes rather than a 404 when
            // hitting the root or a directory of a site, where there is no default page or
            // no extension in the path requested.

            var responseStream = response.ToStream();
            if (responseStream is not null && responseStream.Length == 0)
            {
                response.StatusCode = 404;
            }

            return;
        }

        foreach (var locationHeader in location)
        {
            if (string.IsNullOrWhiteSpace(locationHeader))
            {
                continue;
            }

            var locationUri = new Uri(locationHeader, UriKind.RelativeOrAbsolute);
            if (locationUri.IsAbsoluteUri &&
                routeInfo.BlockVersionInfo != null &&
                locationUri.Host.Contains(routeInfo.BlockVersionInfo.BaseUri.Host))
            {
                var newQueryString = HttpUtility.ParseQueryString(locationUri.Query);
                RestoreOrRemoveInjectedParam(context, newQueryString, "nodeId");
                RestoreOrRemoveInjectedParam(context, newQueryString, "entryId");
                var updatedUri = locationUri.AbsolutePath;
                if (newQueryString.Count > 0)
                {
                    updatedUri += $"?{newQueryString}";
                }

                response.Headers["location"] = new List<string>
                {
                    updatedUri
                };
            }
            else
            {
                response.Headers["location"] = new List<string>
                {
                    locationUri.ToString()
                };
            }
        }
    }

    private void SetResponseHeaders(HttpContext context, Dictionary<string, IEnumerable<string>> responseMessage)
    {
        // Copy response headers from primary (top-level pagelet) response (for now).
        foreach (var (key, value) in responseMessage)
        {
            context.Response.Headers[key] = value.ToArray();
        }

        // Set surrogate key response
        EnsureSurrogateKey(context, Constants.Headers.SurrogateKey, cacheKeyService.GetSurrogateKey());
        EnsureRequiresHeaders(context);

        context.Response.Headers.Remove(Constants.Headers.TransferEncoding);

        if (context.Request.Headers[Constants.Headers.Debug] == "true" ||
            context.Request.Headers[Constants.Headers.AltDebug] == "true" ||
            context.Request.Headers["echo-headers"] == "true")
        {
            if (context.Request.Headers[Constants.Headers.AltDebug] != "true")
            {
                // Set debug surrogate key response
                EnsureSurrogateKey(
                    context,
                    Constants.Headers.DebugSurrogateKey,
                    cacheKeyService.GetDebugSurrogateKey());
            }

            foreach (var requestHeader in context.Request.Headers)
            {
                try
                {
                    if (!Constants.Headers.RequiresHeaders.ContainsCaseInsensitive(requestHeader.Key))
                    {
                        context.Response.Headers.TryAdd(requestHeader.Key, requestHeader.Value);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error encountered while reading headers");
                }
            }
        }
    }

    private void EnsureSurrogateKey(HttpContext context, string surrogateKeyHeader, string surrogateKey)
    {
        if (!string.IsNullOrWhiteSpace(surrogateKey))
        {
            if (surrogateKey == Constants.CacheKeys.AnyUpdate)
            {
            }
            else
            {
                context.Response.Headers[surrogateKeyHeader] = surrogateKey;
            }
        }
    }

    private void EnsureRequiresHeaders(HttpContext context)
    {
        foreach (var requiresHeader in Constants.Headers.RequiresHeaders)
        {
            if (CallContext.Current.Values.ContainsKey(requiresHeader))
            {
                var requiredHeader =
                    requiresHeader.Replace("-requires", "", StringComparison.InvariantCultureIgnoreCase);

                context.Response.Headers[requiredHeader] = CallContext.Current[requiresHeader];
            }
        }
    }
}