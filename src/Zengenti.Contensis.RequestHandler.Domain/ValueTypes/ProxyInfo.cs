namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public record ProxyInfo(Guid ProxyId, bool ParseContent, bool IsPartialMatchPath);