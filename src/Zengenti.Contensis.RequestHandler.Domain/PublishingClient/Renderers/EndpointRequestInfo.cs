namespace Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;

public class EndpointRequestInfo
{
    public EndpointRequestInfo(
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
        List<string> cacheKeys)
    {
        BlockId = blockId;
        BlockVersionId = blockVersionId;
        EndpointId = endpointId;
        LayoutRendererId = layoutRendererId;
        Uri = uri;
        StaticPaths = staticPaths;
        RendererId = rendererId;
        Headers = headers;
        Branch = branch;
        BlockVersionNo = blockVersionNo;
        EnableFullUriRouting = enableFullUriRouting;
        CacheKeys = cacheKeys;
    }

    public string BlockId { get; }
    public Guid? BlockVersionId { get; }
    public string EndpointId { get; }
    public Guid? LayoutRendererId { get; }
    public string Uri { get; }
    public List<string> StaticPaths { get; }
    public Guid? RendererId { get; }
    public Dictionary<string, string> Headers { get; }
    public string Branch { get; }
    public int? BlockVersionNo { get; }
    public bool EnableFullUriRouting { get; }
    public List<string> CacheKeys { get; }
}