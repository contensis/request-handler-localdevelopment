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


        var curlUri = new UriBuilder(routeInfo.Uri) { Host = "localhost" };
        var curlString = $"curl '{curlUri}' \n";
        var disallowedHeaders = EndpointRequestService.DisallowedRequestHeaders;
        foreach (var header in routeInfo.Headers.Values)
        {
            if (disallowedHeaders.ContainsCaseInsensitive(header.Key)){
                continue;
            }
            curlString += $"  -H '{header.Key}: {header.Value}' \n";

        }
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
        else
        {
            return "";
        }

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
        else
        {
            return "";
        }

    }
}
