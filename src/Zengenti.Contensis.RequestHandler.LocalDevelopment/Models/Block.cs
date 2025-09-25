namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

public class Block
{
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public string? Id { get; set; }

    public Uri? BaseUri { get; set; }

    public bool? EnableFullUriRouting { get; set; }

    public List<string> StaticPaths { get; set; } = new();

    public List<Endpoint> Endpoints { get; set; } = new();

    public int VersionNo { get; set; }

    public string Branch { get; set; } = "";

    public DateTime? Pushed { get; set; }
}