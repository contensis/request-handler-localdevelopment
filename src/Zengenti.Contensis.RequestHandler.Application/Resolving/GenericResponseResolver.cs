using System.Text;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Resolving;

public class GenericResponseResolver : IResponseResolver
{
    public async Task<string> Resolve(string content, RouteInfo routeInfo, int currentDepth, CancellationToken ct)
    {
        return await Task.Run(() =>
            ReplaceStaticPaths(content, routeInfo.RoutePrefix, routeInfo.BlockVersionInfo?.StaticPaths.ToArray()), ct);
    }

    private static string ReplaceStaticPaths(string content, string endpointPrefix, string[]? staticPaths)
    {
        if (staticPaths == null || staticPaths.Length == 0)
        {
            return content;
        }

        // TODO: Consider https://stackoverflow.com/questions/20220913/fastest-way-to-replace-multiple-strings-in-a-huge-string
        var sb = new StringBuilder(content);

        foreach (var staticPath in staticPaths)
        {
            var staticPathToReplace = $"{staticPath.Trim('/')}/";
            var staticPathReplacement = $"{endpointPrefix}/{staticPath.Trim('/')}/";

            sb.Replace(staticPathToReplace, staticPathReplacement);

            // The path maybe encoded by React in JSON
            var encodedStaticPathToReplace = staticPathToReplace.Replace("/", "\\u002F");
            var encodedStaticPathReplacement = staticPathReplacement.Replace("/", "\\u002F");
            sb.Replace(encodedStaticPathToReplace, encodedStaticPathReplacement);
        }

        return sb.ToString();
    }
}