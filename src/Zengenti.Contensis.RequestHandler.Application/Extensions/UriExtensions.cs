namespace Zengenti.Contensis.RequestHandler.Application;

public static class UriExtensions
{
    public static bool EndsWithForwardSlash(this Uri? uri)
    {
        return uri != null && uri.AbsolutePath.EndsWith('/');
    }
}