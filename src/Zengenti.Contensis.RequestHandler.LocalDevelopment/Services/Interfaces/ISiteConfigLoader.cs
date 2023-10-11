using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;

public interface ISiteConfigLoader
{
    SiteConfig SiteConfig { get; }
}