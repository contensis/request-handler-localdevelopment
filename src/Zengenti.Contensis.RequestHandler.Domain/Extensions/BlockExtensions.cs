using Zengenti.Contensis.RequestHandler.Domain.Entities;

namespace Zengenti.Contensis.RequestHandler.Domain.Extensions;

public static class BlockExtensions
{
    public static void EnsureDefaultStaticPaths(this BlockVersionInfo blockVersionInfo)
    {
        AddToStaticPaths(blockVersionInfo.StaticPaths);
    }

    public static void AddToStaticPaths(List<string>? staticPaths)
    {
        if (staticPaths == null)
        {
            return;
        }
            
        var defaultStaticPath = "/static";
        if (!staticPaths.ContainsCaseInsensitive(defaultStaticPath))
        {
            staticPaths.Add(defaultStaticPath);
        }
    }
}