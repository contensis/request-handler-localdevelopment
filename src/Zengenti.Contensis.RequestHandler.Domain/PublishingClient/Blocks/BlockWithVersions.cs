namespace Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Blocks;

public class BlockWithVersions
{
    public BlockWithVersions(string id, string description)
    {
        Id = id;
        Description = description;
        Branches = new List<BranchWithVersions>();
    }

    public string Id { get; }

    public string Description { get; }

    public IList<BranchWithVersions> Branches { get; }
}