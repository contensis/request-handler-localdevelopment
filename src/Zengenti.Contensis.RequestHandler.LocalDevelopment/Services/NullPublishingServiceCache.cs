using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

/// <summary>
///     We don't cache anything in local development mode
/// </summary>
public class NullPublishingServiceCache : IPublishingServiceCache
{
    public BlockVersionInfo? GetBlockVersionInfo(Guid id)
    {
        return null;
    }

    public void SetBlockVersionInfo(BlockVersionInfo blockVersionInfo)
    {
    }
}