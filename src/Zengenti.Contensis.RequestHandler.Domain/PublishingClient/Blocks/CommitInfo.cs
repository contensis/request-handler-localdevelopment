namespace Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Blocks;

public record CommitInfo
{
    public string Id { get; set; }

    public string Message { get; set; }

    public DateTime DateTime { get; set; }

    public string AuthorEmail { get; set; }

    public string CommitUrl { get; set; }
}