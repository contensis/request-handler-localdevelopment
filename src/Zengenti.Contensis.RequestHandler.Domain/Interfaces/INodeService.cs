using Zengenti.Contensis.RequestHandler.Domain.Entities;

namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

public interface INodeService
{
    Task<Node?> GetByPath(string path);
}