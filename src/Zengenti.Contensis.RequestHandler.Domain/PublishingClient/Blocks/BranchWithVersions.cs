namespace Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Blocks;

public class BranchWithVersions
{
    public BranchWithVersions(string id)
    {
        Id = id;
        Versions = new List<BlockVersionForUi>();
    }

    public string Id { get; }

    public List<BlockVersionForUi> Versions { get; }
}