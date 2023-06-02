using Zengenti.Contensis.RequestHandler.Domain.Interfaces;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public class NullCacheKeyService: ICacheKeyService
{
    public void Add(string key)
    {
    }

    public void Add(Dictionary<string, IEnumerable<string>> headers)
    {
    }

    public void AddRange(IEnumerable<string> keys)
    {
    }

    public string GetSurrogateKey()
    {
        return string.Empty;
    }

    public string GetDebugSurrogateKey()
    {
        return string.Empty;
    }
}