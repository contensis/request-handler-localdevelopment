using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;

namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public class RequestContext(Guid projectUuid)
{
    public Guid ProjectUuid { get; set; } = projectUuid;

    public Guid? ContentTypeId { get; set; }

    public string RendererId { get; set; } = "";

    public Guid? ProxyId { get; set; }

    public string Language { get; set; } = "";

    public bool IsPartialMatchPath { get; set; }

    public string ProxyVersionConfig { get; set; } = "";

    public string BlockVersionConfig { get; set; } = "";

    public string RendererVersionConfig { get; set; } = "";

    public ServerType ServerType { get; set; }
}