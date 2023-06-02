using Zengenti.Contensis.RequestHandler.Domain.Interfaces;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public class HttpGlobalApi : IGlobalApi
{
    public Task<bool> IsContensisSingleSignOn()
    {
        return Task.FromResult(false);
    }
}