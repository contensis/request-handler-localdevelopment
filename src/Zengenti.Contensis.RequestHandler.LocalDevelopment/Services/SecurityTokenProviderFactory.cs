using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;
using Zengenti.Security;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public class SecurityTokenProviderFactory : ISecurityTokenProviderFactory
{
    public ISecurityTokenProvider GetSecurityTokenProvider(SecurityTokenParams securityTokenParams)
    {
        if (!string.IsNullOrEmpty(securityTokenParams.ClientId))
        {
            return new ClientCredentialsSecurityTokenProvider(securityTokenParams);
        }

        return new ContensisClassicSecurityTokenProvider(securityTokenParams);
    }
}