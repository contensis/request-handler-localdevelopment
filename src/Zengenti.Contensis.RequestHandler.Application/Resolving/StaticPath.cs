using Zengenti.Contensis.RequestHandler.Domain.Common;

namespace Zengenti.Contensis.RequestHandler.Application.Resolving;

/// <summary>
///     A helper class that understands the structure of re-written static file paths.
/// </summary>
public class StaticPath
{
    private static readonly int PrefixLength = Constants.Paths.StaticPathUniquePrefix.Length * 2 + 6;

    private StaticPath(string path)
    {
        Path = path;
        OriginalPath = path;
    }

    /// <summary>
    ///     The prefix Guid value.
    /// </summary>
    public Guid BlockVersionId { get; private set; }

    /// <summary>
    ///     Whether the path has actually been re-written (prefix value exists).
    /// </summary>
    public bool IsRewritten => BlockVersionId != Guid.Empty;

    /// <summary>
    ///     The un-parsed path - i.e. the path passed-in.
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///     The path without the prefix.
    /// </summary>
    public string OriginalPath { get; private set; }

    /// <summary>
    ///     Parses a given string into a StaticPath instance.
    /// </summary>
    /// <param name="path">The path to parse.</param>
    /// <returns>The StaticPath instance.</returns>
    public static StaticPath? Parse(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var staticPath = new StaticPath(path);
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > 0)
        {
            // A re-written path will look like "/{prefix}Gp98VQ{prefix}a54f38c2-16a5-4399-bf17-bf2d93a3badf/images/header.png"
            var prefixPart = parts[0];
            if (prefixPart.StartsWith(Constants.Paths.StaticPathUniquePrefix) &&
                prefixPart.Length > PrefixLength &&
                Guid.TryParse(prefixPart.Substring(PrefixLength), out var prefixValue))
            {
                staticPath.BlockVersionId = prefixValue;
                staticPath.OriginalPath =
                    "/" + string.Join('/', parts.Skip(1).Take(parts.Length - 1)); // Not sure this is right....
            }
        }

        return staticPath;
    }
}