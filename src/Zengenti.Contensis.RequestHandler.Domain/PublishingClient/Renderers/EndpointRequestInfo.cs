namespace Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;

public class EndpointRequestInfo(
    string blockId,
    Guid? blockVersionId,
    string endpointId,
    Guid? layoutRendererId,
    string uri,
    List<string> staticPaths,
    Guid? rendererId,
    Dictionary<string, string> headers,
    string branch,
    int? blockVersionNo,
    bool enableFullUriRouting,
    DateTime? pushed,
    List<string> cacheKeys)
{
    public string BlockId { get; } = blockId;
    public Guid? BlockVersionId { get; } = blockVersionId;
    public string EndpointId { get; } = endpointId;
    public Guid? LayoutRendererId { get; } = layoutRendererId;
    public string Uri { get; } = uri;
    public List<string> StaticPaths { get; } = staticPaths;
    public Guid? RendererId { get; } = rendererId;
    public Dictionary<string, string> Headers { get; } = headers;
    public string Branch { get; } = branch;
    public int? BlockVersionNo { get; } = blockVersionNo;
    public bool EnableFullUriRouting { get; } = enableFullUriRouting;

    public DateTime? Pushed { get; set; } = pushed;
    public List<string> CacheKeys { get; } = cacheKeys;
}