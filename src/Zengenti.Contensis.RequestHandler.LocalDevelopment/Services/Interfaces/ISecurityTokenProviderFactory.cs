using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Security;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;

public interface ISecurityTokenProviderFactory
{
    ISecurityTokenProvider GetSecurityTokenProvider(SecurityTokenParams securityTokenParams);
}