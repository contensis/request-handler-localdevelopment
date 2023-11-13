using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Blocks;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

public interface IPublishingApi
{
    Task<BlockVersionInfo?> GetBlockVersionInfo(Guid versionId);

    Task<EndpointRequestInfo?> GetEndpointForRequest(RequestContext requestContext);

    Task<IList<BlockWithVersions>> ListBlocksThatAreAvailable(
        Guid projectUuid,
        string viewType,
        string activeBlockVersionConfig,
        string defaultBlockVersionConfig,
        string serverType);
}