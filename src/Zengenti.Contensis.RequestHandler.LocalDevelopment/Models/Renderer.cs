namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

public class Renderer
{
    private readonly Guid _defaultUuid = Guid.NewGuid();
    private Guid? _uuid;

    public Guid? Uuid
    {
        get => _uuid ?? _defaultUuid;
        set => _uuid = value ?? _defaultUuid;
    }

    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public List<RendererRule> Rules { get; set; } = new List<RendererRule>();

    public string LayoutRenderer { get; set; } = null!;

    public Guid? LayoutRendererId { get; set; } = null!;

    public List<string> AssignedContentTypes { get; set; } = new List<string>();

    public EndpointRef? ExecuteRules()
    {
        if (Rules.Count > 0)
        {
            return Rules[0].Return;
        }

        return null;
    }
}