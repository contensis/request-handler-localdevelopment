using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Services;

public class ServerTypeResolver : IServerTypeResolver
{
    public ServerType GetServerType()
    {
        var serverTypeAsString = CallContext.Current[Constants.Headers.ServerType];
        if (string.IsNullOrWhiteSpace(serverTypeAsString))
        {
            return ServerType.Live;
        }

        if (serverTypeAsString.ToLowerInvariant() == "test")
        {
            serverTypeAsString = "preview";
        }

        if (Enum.TryParse<ServerType>(serverTypeAsString, true, out var serverType))
        {
            return serverType;
        }

        return ServerType.Live;
    }
}