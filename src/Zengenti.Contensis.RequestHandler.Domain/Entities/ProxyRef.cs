namespace Zengenti.Contensis.RequestHandler.Domain.Entities;

public class ProxyRef
{
    public Guid Id { get; set; }

    public bool ParseContent { get; set; }

    public bool PartialMatch { get; set; }
}