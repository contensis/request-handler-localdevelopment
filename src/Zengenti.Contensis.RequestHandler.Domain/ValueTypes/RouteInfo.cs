using System.Diagnostics;
using System.Web;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;

namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

/// <summary>
///     Contains the information required to invoke a block or proxied call.
/// </summary>
[DebuggerDisplay("Uri: {Uri}")]
public class RouteInfo
{
    public RouteInfo(
        Uri uri,
        Headers headers,
        string nodePath,
        bool foundRoute,
        BlockVersionInfo? blockVersionInfo = null,
        string? endpointId = null,
        Guid? layoutRendererId = null,
        bool parseContent = false,
        Guid? proxyId = null,
        bool isIisFallback = false)
    {
        Uri = uri;
        Headers = headers;
        BlockVersionInfo = blockVersionInfo;
        EndpointId = endpointId;
        LayoutRendererId = layoutRendererId;
        ParseContent = parseContent;
        IsIisFallback = isIisFallback;
        Metrics = new Metrics();
        NodePath = nodePath;
        FoundRoute = foundRoute;
        ProxyId = proxyId;
        var hashedProjectUuid = GetUrlFriendlyHash(BlockVersionInfo?.ProjectUuid);
        RoutePrefix =
            $"{Constants.Paths.StaticPathUniquePrefix}{hashedProjectUuid}{Constants.Paths.StaticPathUniquePrefix}{BlockVersionInfo?.BlockVersionId}";
    }

    public static string GetUrlFriendlyHash(Guid? guid)
    {
        var hashedGuid = Keys.Hash(guid.ToString() ?? "");
        if (hashedGuid != HttpUtility.UrlEncode(hashedGuid))
        {
            hashedGuid = hashedGuid.Replace("/", "+");
        }

        return hashedGuid;
    }

    /// <summary>
    ///     The full calculated URI to invoke.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    ///     The request headers.
    /// </summary>
    public Headers Headers { get; }

    /// <summary>
    ///     The block version information required to invoke a running block.
    /// </summary>
    public BlockVersionInfo? BlockVersionInfo { get; }

    /// <summary>
    ///     Maintained for information and error logging.
    /// </summary>
    public string? EndpointId { get; }

    /// <summary>
    ///     Contextual layout renderer identifier.
    /// </summary>
    public Guid? LayoutRendererId { get; }

    /// <summary>
    ///     Whether the content served by the proxy should be parsed
    /// </summary>
    public bool ParseContent { get; }

    /// <summary>
    ///     The unique identifier of the proxy
    /// </summary>
    public Guid? ProxyId { get; }

    /// <summary>
    ///     The static route prefix for a block
    /// </summary>
    public string RoutePrefix { get; init; }

    /// <summary>
    ///     Whether the route is falling back and reverse proxying to an IIS site
    /// </summary>
    public bool IsIisFallback { get; }

    /// <summary>
    ///     Metrics collected along the pipeline.
    /// </summary>
    public Metrics Metrics { get; }

    /// <summary>
    ///     Maintained to enable friendly error messages.
    /// </summary>
    public string? NodePath { get; }

    /// <summary>
    ///     Maintained to enable friendly error messages.
    /// </summary>
    public bool FoundRoute { get; }
}