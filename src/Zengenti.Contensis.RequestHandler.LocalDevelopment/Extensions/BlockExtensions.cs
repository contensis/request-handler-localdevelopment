using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Extensions;

public static class BlockExtensions
{
    public static void EnsureDefaultStaticPaths(this Block? block)
    {
        if (block == null)
        {
            return;
        }

        Domain.Extensions.BlockExtensions.AddToStaticPaths(block.StaticPaths);
    }
}