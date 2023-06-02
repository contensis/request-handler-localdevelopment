namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

public interface IGlobalApi
{
    Task<bool> IsContensisSingleSignOn();
}