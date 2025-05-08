namespace Zengenti.Contensis.RequestHandler.Domain.Entities;

public class Node
{
#pragma warning disable CS8618
    public Node()
    {
        // For YAML serialization when in local development mode.
    }
#pragma warning restore CS8618

    public Node(
        string path,
        Guid? id,
        Guid? entryId,
        Guid? contentTypeId,
        IEnumerable<string> cacheKeys,
        Guid? rendererId,
        bool? isPartialMatchRoot,
        string language,
        Guid? proxyId,
        bool? parseContent,
        bool? isPartialMatchProxy)
    {
        Id = id;
        EntryId = entryId;
        ContentTypeId = contentTypeId;
        Path = path;
        CacheKeys = cacheKeys;

        if (rendererId.HasValue)
        {
            RendererRef = new RendererRef
            {
                Uuid = rendererId.Value,
                IsPartialMatchRoot = isPartialMatchRoot.GetValueOrDefault()
            };
        }

        if (proxyId.HasValue)
        {
            ProxyRef = new ProxyRef
            {
                Id = proxyId.Value,
                ParseContent = parseContent.GetValueOrDefault(),
                PartialMatch = isPartialMatchProxy.GetValueOrDefault()
            };
        }

        Language = language;
    }

    public Guid? Id { get; set; }

    public Guid? EntryId { get; set; }

    public Guid? ContentTypeId { get; set; }

    // For local development resolving
    public string ContentTypeApiId { get; set; } = null!;

    public string Path { get; set; } = null!;

    public IEnumerable<string> CacheKeys { get; set; } = null!;

    public RendererRef? RendererRef { get; set; }

    /// <summary>
    ///     Internal use for test support
    /// </summary>
    public RendererRef? Renderer
    {
        set => RendererRef = value;
    }

    public ProxyRef? ProxyRef { get; set; }

    /// <summary>
    ///     /// Internal use for test support
    /// </summary>
    public ProxyRef? Proxy
    {
        set => ProxyRef = value;
    }

    public string Language { get; set; }
}