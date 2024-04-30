namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public record BlockClusterConfig(
    string? BlockClusterIngressIp = null,
    string? BlockAddressSuffix = null,
    string[]? AliasesWithApiRoutes = null);