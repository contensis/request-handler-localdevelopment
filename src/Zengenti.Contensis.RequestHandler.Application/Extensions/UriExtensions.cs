using Zengenti.Contensis.RequestHandler.Domain.Common;

namespace Zengenti.Contensis.RequestHandler.Application;

public static class UriExtensions
{
    public static bool EndsWithForwardSlash(this Uri uri)
    {
        return uri.AbsolutePath != "/" && uri.AbsolutePath.EndsWith('/');
    }

    public static bool IsContensisApiRequest(this Uri uri)
    {
        var path = uri.AbsolutePath;
        var isContensisApiRequest =
            Constants.Paths.ApiPrefixes.Any(prefix => path.StartsWithCaseInsensitive(prefix)) &&
            !path.StartsWithCaseInsensitive("/api/publishing/request-handler") &&
            !path.StartsWithCaseInsensitive("/api/preview-toolbar/blocks");
        return isContensisApiRequest;
    }
}