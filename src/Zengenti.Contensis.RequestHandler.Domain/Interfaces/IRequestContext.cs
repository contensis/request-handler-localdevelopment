using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Proxies;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

public interface IRequestContext
{
    public bool TraceEnabled { get; }

    public string Alias { get; }

    public string ProjectApiId { get; }

    public Guid ProjectUuid { get; }

    //public Uri ApiUri { get; }

    public VersionStatus NodeVersionStatus { get; }

    public string BlockConfig { get; }

    public string RendererConfig { get; }

    public string ProxyConfig { get; }
}