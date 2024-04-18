using System.Reflection;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Middleware;

public static class ErrorResources
{
    public static string GetMessage(int statusCode, RouteInfo routeInfo)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Zengenti.Contensis.RequestHandler.Application.ErrorMessages.{statusCode}.html";

        var curlString = CreateCurlCallString(routeInfo, "localhost");

        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream != null)
        {
            var textStreamReader = new StreamReader(resourceStream);
            var message = textStreamReader.ReadToEnd();
            message = message.Replace("$urlRequested", routeInfo.Uri.PathAndQuery);
            message = message.Replace("$blockId", routeInfo.BlockVersionInfo?.BlockId);
            message = message.Replace("$branch", routeInfo.BlockVersionInfo?.Branch);
            message = message.Replace("$blockVersionId", routeInfo.BlockVersionInfo?.BlockVersionId.ToString());
            message = message.Replace("$versionNo", routeInfo.BlockVersionInfo?.VersionNo.ToString());
            message = message.Replace("$fullUri", routeInfo.BlockVersionInfo?.BaseUri.ToString());
            message = message.Replace("$localCurlString", curlString);

            return message;
        }

        return "";
    }

    internal static string CreateCurlCallString(RouteInfo routeInfo, string? host = "")
    {
        var curlUri = new UriBuilder(routeInfo.Uri);

        if (!string.IsNullOrWhiteSpace(host))
        {
            curlUri.Host = host;
        }

        var curlString = $"curl '{curlUri}' \n";
        var disallowedHeaders = EndpointRequestService.DisallowedRequestHeaders;
        foreach (var header in routeInfo.Headers.Values)
        {
            if (disallowedHeaders.ContainsCaseInsensitive(header.Key) || !header.Value.Any())
            {
                continue;
            }

            var headerValue = header.Value.Count() == 1 ?  header.Value.First() : string.Join("; ", header.Value);
            curlString += $"  -H '{header.Key}: {headerValue}' \n";
        }

        return curlString;
    }

    public static string GetIisFallbackMessage(int statusCode, RouteInfo routeInfo, string nodePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Zengenti.Contensis.RequestHandler.Application.ErrorMessages.{statusCode}_iis.html";

        var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream != null)
        {
            var textStreamReader = new StreamReader(resourceStream);
            var message = textStreamReader.ReadToEnd();
            message = message.Replace("$urlRequested", routeInfo.Uri.PathAndQuery);
            message = message.Replace("$urlPathRequested", routeInfo.Uri.AbsolutePath);
            message = message.Replace("$nodePath", nodePath);

            return message;
        }

        return "";
    }
}