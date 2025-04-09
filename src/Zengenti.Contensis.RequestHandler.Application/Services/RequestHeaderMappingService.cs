namespace Zengenti.Contensis.RequestHandler.Application.Services;

public class RequestHeaderMappingService
{
    public static readonly string[] DisallowedRequestHeaderMappings =
    [
        "Host", // Will be explicitly set
        "Accept-Encoding", // Don't compress endpoint traffic as compression will be done in Varnish
        "version", // Don't pass on a version header because we have to set a version header to blocks themselves
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

    public static readonly string[] AllowedEntityHeaders =
    [
        "Allow",
        "Content-Encoding",
        "Content-Language",
        "Content-Length",
        "Content-Location",
        "Content-MD5",
        "Content-Range",
        "Content-Type",
        "Expires",
        "Last-Modified"
    ];

    public void MapHeaders(HttpRequestMessage requestMessage, Dictionary<string, IEnumerable<string>> headers)
    {
        foreach (var (key, value) in headers)
        {
            var valueArray = value as string[] ?? value.ToArray();

            if (IsAllowedRequestHeader(key))
            {
                requestMessage.Headers.TryAddWithoutValidation(key, valueArray);
            }

            if (requestMessage.Content != null && IsEntityHeader(key))
            {
                requestMessage.Content.Headers.TryAddWithoutValidation(key, valueArray);
            }
        }
    }

    private static bool IsAllowedRequestHeader(string key)
    {
        return !DisallowedRequestHeaderMappings.ContainsCaseInsensitive(key);
    }

    private bool IsEntityHeader(string key)
    {
        return IsAllowedRequestHeader(key) && AllowedEntityHeaders.ContainsCaseInsensitive(key);
    }
}