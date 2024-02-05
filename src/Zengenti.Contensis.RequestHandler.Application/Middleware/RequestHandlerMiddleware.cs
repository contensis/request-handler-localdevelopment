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

public class RequestHandlerMiddleware
{
    private readonly RequestDelegate _nextMiddleware;
    private readonly IRouteInfoFactory _routeInfoFactory;
    private readonly ILogger<RequestHandlerMiddleware> _logger;
    private readonly ICacheKeyService _cacheKeyService;
    private readonly IGlobalApi _globalApi;
    private readonly CallContextService _callContextService;
    private static readonly ActivitySource ActivitySource = new("Zengenti.Contensis.RequestHandler.Middleware");

    private static readonly string[] ExcludedPaths =
    {
        "/api/preview-toolbar/blocks",
        "/pingz",
        "/healthz",
        "/infoz",
        "/livez"
    };

    private readonly IRequestContext _requestContext;

    public IEndpointRequestService RequestService { get; }

    internal IRouteService RouteService { get; }

    public RequestHandlerMiddleware(
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
        _nextMiddleware = nextMiddleware;
        _requestContext = requestContext;
        RouteService = routeService;
        _routeInfoFactory = routeInfoFactory;
        _cacheKeyService = cacheKeyService;
        RequestService = endpointRequestService;
        _globalApi = globalApi;
        _callContextService = callContextService;
        _logger = logger;
    }

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

            await _nextMiddleware(context);
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

            using (_logger.BeginScope(
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
                _logger.LogError(
                    e,
                    "Unhandled error caught in middleware with exception message {Message} and request url {Url}",
                    e.Message,
                    context.Request.GetDisplayUrl());
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
            if (query.Value.Count > 0 && _callContextService.IsVersionConfigKey(key))
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
        _callContextService.SetCallContextData(request);
        activity?.SetTag("alias", CallContext.Current[Constants.Headers.Alias]);
        activity?.SetTag("projectUuid", CallContext.Current[Constants.Headers.ProjectUuid]);
        activity?.SetTag("projectApiId", CallContext.Current[Constants.Headers.ProjectApiId]);
        activity?.SetTag("blockVersionConfig", CallContext.Current[Constants.Headers.BlockConfig]);
        activity?.SetTag("proxyConfig", CallContext.Current[Constants.Headers.ProxyConfig]);
        activity?.SetTag("rendererConfig", CallContext.Current[Constants.Headers.RendererConfig]);
        activity?.SetTag("nodeConfig", CallContext.Current[Constants.Headers.NodeVersionStatus]);
    }

    private RouteInfo TryToCreateIisFallbackRouteInfo(HttpContext context, Headers headers)
    {
        if (!string.IsNullOrWhiteSpace(_requestContext.IisHostname) &&
            !string.IsNullOrWhiteSpace(_requestContext.LoadBalancerVip))
        {
            return _routeInfoFactory.CreateForIisFallback(
                context.Request.GetOriginUri(),
                headers);
        }

        // TODO: the Uri property is expected to be null - we need to change it to be nullable
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        return new RouteInfo(null, headers, "", false);
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
        if (routeInfo.FoundRoute == false)
        {
            // No route info returned (No node/block/renderer/proxy found) so try and fallback to the IIS site if specified
            routeInfo = TryToCreateIisFallbackRouteInfo(context, headers);
            if (routeInfo.FoundRoute == false)
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

        var response = await RequestService.Invoke(
            context.Request.GetHttpMethod(),
            context.Request.Body,
            headers,
            routeInfo,
            0,
            context.RequestAborted);

        responseTimer.Stop();

        var fallbackTimer = new Stopwatch();
        fallbackTimer.Start();
        if (!routeInfo.IsIisFallback && response.StatusCode == (int)HttpStatusCode.NotFound)
        {
            // Block/proxy request returned 404 so try and fallback to the IIS site if specified
            var fallbackRouteInfo = TryToCreateIisFallbackRouteInfo(context, headers);
            if (fallbackRouteInfo.FoundRoute)
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

        FixIisStatusCode(routeInfo, response);

        AddCacheHeadersFor404Errors(response, context);

        fallbackTimer.Stop();
        var performanceLog = GetPerformanceInfo(routeInfoTimer, responseTimer, fallbackTimer, mainRouteInfoMetrics);
        if (response.Headers.ContainsKey("request-handler-metrics"))
        {
            response.Headers.Remove("request-handler-metrics");
        }

        response.Headers.Add(
            "request-handler-metrics",
            new List<string>
            {
                performanceLog
            });

        UpdateLocationResponseHeader(context, response, routeInfo);

        return response;
    }

    private EndpointResponse? GetRedirectResponseIfRequired(HttpContext context, Headers headers)
    {
        var queryStringBlockConfigData = GetConfigCookieCollection(context.Request);

        if (queryStringBlockConfigData.Count == 0)
        {
            return null;
        }

        string blockConfigCookie = string.Empty;
        string rendererConfigCookie = string.Empty;
        string proxyConfigCookie = string.Empty;
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

        var redirectResponse = new EndpointResponse("", headers, 301);

        var newQueryString = HttpUtility.ParseQueryString(context.Request.QueryString.Value ?? "");

        var keysToRemove = new List<string>();
        foreach (string queryKey in newQueryString.Keys)
        {
            if (_callContextService.IsVersionConfigKey(queryKey))
            {
                keysToRemove.Add(queryKey);
            }
        }

        foreach (string keyToRemove in keysToRemove)
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
        context.Response.StatusCode = response.StatusCode;

        // Headers
        SetResponseHeaders(context, response.Headers);

        // The content may have changed due to resolving pagelets and re-writing static paths, so get the actual length.
        var responseContent = response.ToStream();
        if (responseContent is MemoryStream)
        {
            context.Response.ContentLength = responseContent.Length;
        }

        if (responseContent != null)
        {
            await responseContent.CopyToAsync(context.Response.Body);
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

        if (routeInfo.IsIisFallback)
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
            var isContensisSingleSignOn = await _globalApi.IsContensisSingleSignOn();
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
            response.Headers,
            response.StatusCode,
            response.PageletPerformanceData);
        // This ensures varnish will not replace our 503 with its own.
        if (response.Headers.ContainsKey("expose-raw-errors"))
        {
            response.Headers.Remove("expose-raw-errors");
        }

        response.Headers.Add(
            "expose-raw-errors",
            new List<string>
            {
                "True"
            });

        response.Headers.Add(
            "content-type",
            new List<string>
            {
                "text/html; charset=utf-8"
            });
        return response;
    }

    private static void FixIisStatusCode(RouteInfo routeInfo, EndpointResponse response)
    {
        if (routeInfo.IsIisFallback)
        {
            // Unfortunately IIS returns an empty bodied 200 rather than a 404 when
            // hitting the root or a directory of a site, where there is no default page or
            // no extension in the path requested.

            if (response.StatusCode == (int)HttpStatusCode.OK)
            {
                if (response.StreamContent is not null && response.StreamContent.Length == 0)
                {
                    response.StatusCode = 404;
                }
            }
        }
    }

    private static void AddCacheHeadersFor404Errors(EndpointResponse response, HttpContext context)
    {
        if (response.StatusCode == 404 && !context.Response.Headers.ContainsKey("Surrogate-Control"))
        {
            if (context.Request.Path == "/")
            {
                response.Headers.Add(
                    "Surrogate-Control",
                    new List<string>
                    {
                        "max-age=30"
                    });
            }
            else
            {
                //This is to prevent malicious requests bombarding the backend services.
                response.Headers.Add(
                    "Surrogate-Control",
                    new List<string>
                    {
                        "max-age=5"
                    });
            }
        }
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

        if (!response.Headers.ContainsKey("Location"))
        {
            // Unfortunately IIS returns an empty bodied 301 sometimes rather than a 404 when
            // hitting the root or a directory of a site, where there is no default page or
            // no extension in the path requested.

            if (response.StreamContent is not null && response.StreamContent.Length == 0)
            {
                response.StatusCode = 404;
            }

            return;
        }

        var location = response.Headers["Location"];
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
        EnsureSurrogateKey(context, Constants.Headers.SurrogateKey, _cacheKeyService.GetSurrogateKey());
        EnsureRequiresHeaders(context);

        context.Response.Headers.Remove(Constants.Headers.TransferEncoding);

        if (context.Request.Headers[Constants.Headers.Debug] == "true" ||
            context.Request.Headers["echo-headers"] == "true")
        {
            // Set debug surrogate key response
            EnsureSurrogateKey(context, Constants.Headers.DebugSurrogateKey, _cacheKeyService.GetDebugSurrogateKey());

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
                    _logger.LogError(e, "Error encountered while reading headers");
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