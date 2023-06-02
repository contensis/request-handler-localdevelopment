namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

public class ContentTypeRendererMapItem
{
    public string? ContentTypeId { get; set; }

    public Guid ContentTypeUuid { get; set; } = Guid.NewGuid();

    public string? RendererId { get; set; }

    public Guid RendererUuid { get; set; }
}