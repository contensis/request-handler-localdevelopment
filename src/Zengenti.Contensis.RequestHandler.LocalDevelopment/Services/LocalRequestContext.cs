using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Proxies;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public class LocalRequestContext(ISiteConfigLoader siteConfigLoader, bool traceEnabled = true)
    : IRequestContext
{
    public bool TraceEnabled { get; } = traceEnabled;

    public string Alias => siteConfigLoader.SiteConfig.Alias;

    public string ProjectApiId => siteConfigLoader.SiteConfig.ProjectApiId;

    public Guid ProjectUuid => Guid.Empty; // NOT required for local development ATM.

    public VersionStatus NodeVersionStatus =>
        CallContext.Current[Constants.Headers.NodeVersionStatus].EqualsCaseInsensitive("published")
            ? VersionStatus.Published
            : VersionStatus.Latest;

    public string BlockConfig => CallContext.Current[Constants.Headers.BlockConfig];
    public string RendererConfig => CallContext.Current[Constants.Headers.RendererConfig];
    public string ProxyConfig => CallContext.Current[Constants.Headers.ProxyConfig];
    public string IisHostname => siteConfigLoader.SiteConfig.IisHostname ?? "";
    public string LoadBalancerVip => siteConfigLoader.SiteConfig.PodIngressIp ?? "";
}