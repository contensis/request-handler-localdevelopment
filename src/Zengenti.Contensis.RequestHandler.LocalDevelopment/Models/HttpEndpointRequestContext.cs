using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

public class HttpEndpointRequestContext
{
    public string RendererId { get; set; }

    public Guid? ContentTypeId { get; set; }

    public Guid? ProxyId { get; set; }

    public string Language { get; set; }

    public bool IsPartialMatchPath { get; set; }

    public string ProxyVersionConfig { get; set; }

    public string BlockVersionConfig { get; set; }

    public string RendererVersionConfig { get; set; }

    public ServerType ServerType { get; set; }

    public HttpEndpointRequestContext(RequestContext requestContext)
    {
        RendererId = requestContext.RendererId;
        ContentTypeId = requestContext.ContentTypeId;
        ProxyId = requestContext.ProxyId;
        Language = requestContext.Language;
        IsPartialMatchPath = requestContext.IsPartialMatchPath;
        ProxyVersionConfig = requestContext.ProxyVersionConfig;
        BlockVersionConfig = requestContext.BlockVersionConfig;
        RendererVersionConfig = requestContext.RendererVersionConfig;
        ServerType = requestContext.ServerType;
    }
}