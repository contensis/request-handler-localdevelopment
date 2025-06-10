using System.Diagnostics;
using System.Text.Json.Serialization;
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
        RouteType routeType,
        Uri? uri,
        Headers headers,
        string nodePath,
        BlockVersionInfo? blockVersionInfo = null,
        string? endpointId = null,
        Guid? layoutRendererId = null,
        ProxyInfo? proxyInfo = null)
    {
        RouteType = routeType;
        Uri = uri;
        Headers = headers;
        BlockVersionInfo = blockVersionInfo;
        EndpointId = endpointId;
        LayoutRendererId = layoutRendererId;
        ProxyInfo = proxyInfo;

        Metrics = new Metrics();
        DebugData = new DebugData(this);
        NodePath = nodePath;

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
    public Uri? Uri { get; }

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
    ///     The proxy info
    /// </summary>
    public ProxyInfo? ProxyInfo { get; }

    /// <summary>
    ///     The static route prefix for a block
    /// </summary>
    public string RoutePrefix { get; init; }

    /// <summary>
    ///     The route type
    /// </summary>
    public RouteType RouteType { get; }

    /// <summary>
    ///     Metrics collected along the pipeline.
    /// </summary>
    [JsonIgnore]
    public Metrics Metrics { get; }

    /// <summary>
    ///     Debug data collected along the pipeline.
    /// </summary>
    [JsonIgnore]
    public DebugData DebugData { get; }

    /// <summary>
    ///     Maintained to enable friendly error messages.
    /// </summary>
    public string? NodePath { get; }
}