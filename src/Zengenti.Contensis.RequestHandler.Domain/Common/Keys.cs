using System.Data.HashFunction.xxHash;
using System.Text;

namespace Zengenti.Contensis.RequestHandler.Domain.Common;

/// <summary>
///     Builds a small cache (taken from Zengenti.Services.Caching.Keys)
/// </summary>
public static class Keys
{
    private static readonly IxxHash Hasher;

    static Keys()
    {
        Hasher = xxHashFactory.Instance.Create();
    }

    /// <summary>
    ///     Builds the plain text key from the given params, if a part is null an asterix is used.
    /// </summary>
    /// <param name="keyParts">The key parts.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">At least one key part is required - keyParts</exception>
    public static string CreateText(params object[] keyParts)
    {
        if (keyParts == null || keyParts.Length == 0)
        {
            throw new ArgumentException("At least one key part is required", nameof(keyParts));
        }

        return keyParts.Select(k => k.ToString() ?? "*").Aggregate((a, b) => $"{a}_{b}");
    }

    /// <summary>
    ///     Creates cache key from the given params, if a part is null an asterix is used.
    /// </summary>
    /// <param name="keyParts">The key parts.</param>
    /// <returns></returns>
    public static string Create(params object[] keyParts)
    {
        return Hash(CreateText(keyParts));
    }

    /// <summary>
    ///     Returns a hashed cache key for the given key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public static string Hash(string key)
    {
        return Hasher.ComputeHash(Encoding.UTF8.GetBytes(key.ToLowerInvariant())).AsBase64String().TrimEnd('=');
    }
}