using System.Text;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application.Middleware;

public class CallContextService
{
    internal void SetCallContextData(HttpRequest request)
    {
        var headerDictionary = (Dictionary<string, IEnumerable<string>>)new Headers(request.Headers);
        var activeVersionConfigDictionary = new Dictionary<string, string>();
        var defaultVersionConfigDictionary = new Dictionary<string, string>();

        var simpleCookieDictionary = new Dictionary<string, string>(request.Cookies);
        foreach (var configEntry in ExtractConfigDictionary(simpleCookieDictionary, Constants.Headers.ConfigHeaders))
        {
            var key = configEntry.Key.ToLowerInvariant();
            if (IsVersionConfigKey(key))
            {
                activeVersionConfigDictionary.TryAdd(key, configEntry.Value);
            }
        }

        var simpleHeaderDictionary = headerDictionary.ToDictionary(
            keyValuePair =>
                keyValuePair.Key,
            keyValuePair => keyValuePair.Value.FirstOrDefault() ?? "");

        foreach (var configEntry in ExtractConfigDictionary(simpleHeaderDictionary, Constants.Headers.ConfigHeaders))
        {
            activeVersionConfigDictionary.TryAdd(configEntry.Key, configEntry.Value);

            defaultVersionConfigDictionary.TryAdd(configEntry.Key, configEntry.Value);
        }

        CallContext.Current.Clear();

        SetCallContextValueFromRequest(Constants.Headers.Alias, "");
        SetCallContextValueFromRequest(Constants.Headers.ProjectApiId, "");
        SetCallContextValueFromRequest(Constants.Headers.ProjectUuid, Guid.Empty.ToString());

        // Default all requests to published version statuses
        SetCallContextValueFromRequest(Constants.Headers.BlockConfig, "");
        SetCallContextValueFromRequest(Constants.Headers.ProxyConfig, "");
        SetCallContextValueFromRequest(Constants.Headers.RendererConfig, "");
        SetCallContextValueFromRequest(Constants.Headers.BlockConfigDefault, "");
        SetCallContextValueFromRequest(Constants.Headers.ProxyConfigDefault, "");
        SetCallContextValueFromRequest(Constants.Headers.RendererConfigDefault, "");
        SetCallContextValueFromRequest(Constants.Headers.ServerType, ServerType.Live.ToString());
        SetCallContextValueFromRequest(Constants.Headers.TraceEnabled, null);
        SetCallContextValueFromRequest(Constants.Headers.NodeVersionStatus, "published");
        SetCallContextValueFromRequest(Constants.Headers.IisHostname, "");
        SetCallContextValueFromRequest(Constants.Headers.LoadBalancerVip, "");

        // TODO: remove when we deprecate old nodes delivery api
        SetCallContextValueFromRequest(Constants.Headers.UseNewNodeService, "");

        void SetCallContextValueFromRequest(string key, string? alternative)
        {
            bool hasKey = false;
            var isActiveConfigKey = Constants.Headers.ConfigHeaders.Contains(key);
            var isDefaultConfigKey = Constants.Headers.ConfigHeadersWithDefaults.Contains(key);
            if (isActiveConfigKey || isDefaultConfigKey)
            {
                var configPrefix = key.Remove(0, "x-".Length);
                if (isActiveConfigKey)
                {
                    configPrefix = configPrefix.Remove(configPrefix.Length - "config".Length);
                }
                else
                {
                    configPrefix = configPrefix.Remove(configPrefix.Length - "config-default".Length);
                }

                var configValue = ExtractConfigValues(
                    isActiveConfigKey ? activeVersionConfigDictionary : defaultVersionConfigDictionary,
                    configPrefix);

                if (!string.IsNullOrWhiteSpace(configValue))
                {
                    CallContext.Current[key] = configValue;
                    hasKey = true;
                }
            }
            else if (headerDictionary.TryGetValue(key, out var value))
            {
                CallContext.Current[key] = value.ToString();
                hasKey = true;
            }

            if (!hasKey)
            {
                CallContext.Current[key] = alternative;
            }
        }
    }

    public bool IsVersionConfigKey(string key)
    {
        if (key.StartsWithCaseInsensitive("block-") &&
            (key.EndsWithCaseInsensitive("-versionstatus") ||
             key.EndsWithCaseInsensitive("-versionno") ||
             key.EndsWithCaseInsensitive("-branch")))
        {
            return true;
        }

        if ((key.StartsWithCaseInsensitive("proxy-") || key.StartsWithCaseInsensitive("renderer-")) &&
            (key.EndsWithCaseInsensitive("-versionstatus") ||
             key.EndsWithCaseInsensitive("-versionno")))
        {
            return true;
        }

        return false;
    }

    private Dictionary<string, string> ExtractConfigDictionary(
        Dictionary<string, string> sourceDictionary,
        string[] headerConfigKeys)
    {
        var versionConfigEntries = new Dictionary<string, string>();

        foreach (var headerConfigKey in headerConfigKeys)
        {
            if (!sourceDictionary.ContainsKey(headerConfigKey) ||
                string.IsNullOrWhiteSpace(sourceDictionary[headerConfigKey]))
            {
                continue;
            }

            var configParts = sourceDictionary[headerConfigKey].Split("&");
            foreach (var configPart in configParts)
            {
                var keyValueParts = configPart.Split("=");
                if (keyValueParts.Length == 2 && IsVersionConfigKey(keyValueParts[0]))
                {
                    var key = keyValueParts[0].ToLowerInvariant();
                    versionConfigEntries[key] = keyValueParts[1];
                }
            }
        }

        return versionConfigEntries;
    }

    private string ExtractConfigValues(
        Dictionary<string, string> versionConfigDictionary,
        string configPrefix)
    {
        var blockKeyValues = versionConfigDictionary
            .Where(keyValue => keyValue.Key.StartsWithCaseInsensitive($"{configPrefix}"))
            .ToList();
        if (blockKeyValues.Count == 0)
        {
            return "";
        }

        var configBuilder = new StringBuilder();
        foreach (var blockKeyValue in blockKeyValues)
        {
            if (configBuilder.Length > 0)
            {
                configBuilder.Append("&");
            }

            configBuilder.Append($"{blockKeyValue.Key}={blockKeyValue.Value}");
        }

        return configBuilder.ToString();
    }
}