using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

public interface IServerTypeResolver
{
    public ServerType GetServerType();
}