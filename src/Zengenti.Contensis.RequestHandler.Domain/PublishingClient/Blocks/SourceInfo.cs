namespace Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Blocks;

public record SourceInfo
{
    public CommitInfo Commit { get; set; } = null!;
}