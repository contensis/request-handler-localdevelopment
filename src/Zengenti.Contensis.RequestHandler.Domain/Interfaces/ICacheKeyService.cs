namespace Zengenti.Contensis.RequestHandler.Domain.Interfaces;

public interface ICacheKeyService
{
    /// <summary>
    ///     Adds a cache key for local development
    /// </summary>
    /// <param name="key"></param>
    void Add(string key);

    void Add(Dictionary<string, IEnumerable<string>> headers);

    void AddRange(IEnumerable<string> keys);

    string GetSurrogateKey();

    string GetDebugSurrogateKey();
}