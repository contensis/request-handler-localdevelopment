namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

public class EndpointRef
{
    public Guid BlockUuid { get; set; }

    public string? BlockId { get; set; }

    public string? EndpointId { get; set; }

    public string? Version { get; set; }
}