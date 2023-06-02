using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public interface ILocalDevPublishingService : IPublishingService
{
    Guid? GetContentTypeUuid(string id);
    Block? GetBlockById(string id);
}