using System.Net;
using Microsoft.AspNetCore.Http;
using Zengenti.Contensis.RequestHandler.Domain.Common;

namespace Zengenti.Contensis.RequestHandler.Application;

public static class HttpExtensions
{
    public static HttpMethod GetHttpMethod(this HttpRequest request)
    {
        if (HttpMethods.IsDelete(request.Method)) return HttpMethod.Delete;
        if (HttpMethods.IsGet(request.Method)) return HttpMethod.Get;
        if (HttpMethods.IsHead(request.Method)) return HttpMethod.Head;
        if (HttpMethods.IsOptions(request.Method)) return HttpMethod.Options;
        if (HttpMethods.IsPost(request.Method)) return HttpMethod.Post;
        if (HttpMethods.IsPut(request.Method)) return HttpMethod.Put;
        if (HttpMethods.IsTrace(request.Method)) return HttpMethod.Trace;

        return new HttpMethod(request.Method);
    }

    private static readonly string[] ParseableContentTypes =
    {
        Constants.ContentTypes.TextCss,
        Constants.ContentTypes.TextHtml,
        Constants.ContentTypes.ApplicationJson,
        Constants.ContentTypes.ApplicationJavaScript,
        Constants.ContentTypes.ApplicationManifestJson
    };

    public static bool IsResponseResolvable(this HttpResponseMessage? responseMessage)
    {
        if (responseMessage == null || 
            (!responseMessage.IsSuccessStatusCode && responseMessage.StatusCode != HttpStatusCode.NotModified))
        {
            // We may want to use WPP for formatted/container based error responses in the future...
            return false;
        }

        if (responseMessage.Content.Headers.TryGetValues(Constants.Headers.ContentType, 
                out var contentTypes))
        {
            if (ParseableContentTypes.Any(pct => 
                    contentTypes.Any(rct => rct.StartsWithCaseInsensitive(pct))))
            {
                return true;
            }
        }

        return false;
    }

    public static Uri GetOriginUri(this HttpRequest request)
    {
        if (request.Host.HasValue)
        {
            return new Uri($"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}");
        }
            
        return new Uri($"{request.Path}{request.QueryString}");
    }
}