using TestStack.BDDfy;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Application.Services.RequestHeaderMappingServiceStories;

[Story(AsA = "As an EndpointRequestService instance",
    IWant = "I want to have the allowed request headers mapped to the request I am creating",
    SoThat = "So that the request is in the correct state")]
internal abstract class RequestHeaderMappingServiceStory
{
    public static readonly string[] StandardRequestHeaders =
    [
        "Accept",
        "Accept-Charset",
        "Accept-Encoding",
        "Accept-Language",
        "Authorization",
        "Cache-Control",
        "Connection",
        "Cookie",
        "Date",
        "Expect",
        "From",
        "Host",
        "If-Match",
        "If-Modified-Since",
        "If-None-Match",
        "If-Range",
        "If-Unmodified-Since",
        "Max-Forwards",
        "Origin",
        "Pragma",
        "Proxy-Authorization",
        "Range",
        "Referer",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "User-Agent",
        "Via",
        "Warning",
        "X-Requested-With",
        "DNT", // Do Not Track
        "Sec-Fetch-Site",
        "Sec-Fetch-Mode",
        "Sec-Fetch-Dest",
        "Sec-Fetch-User",
        "Sec-CH-UA",
        "Sec-CH-UA-Mobile",
        "Sec-CH-UA-Platform"
    ];
    
    public static readonly string[] StandardEntityHeaders = 
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
}