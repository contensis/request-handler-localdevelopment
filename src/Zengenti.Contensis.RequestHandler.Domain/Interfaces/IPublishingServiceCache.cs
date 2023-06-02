using Zengenti.Contensis.RequestHandler.Domain.Entities;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

public interface IPublishingServiceCache
{
    BlockVersionInfo? GetBlockVersionInfo(Guid id);

    void SetBlockVersionInfo(BlockVersionInfo blockVersionInfo);
}