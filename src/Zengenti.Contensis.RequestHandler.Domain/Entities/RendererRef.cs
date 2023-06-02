namespace Zengenti.Contensis.RequestHandler.Domain.Entities;

public record RendererRef
{
    public Guid Uuid { get; set; }

    public string Id { get; set; } = null!;

    public bool IsPartialMatchRoot { get; set; }
}