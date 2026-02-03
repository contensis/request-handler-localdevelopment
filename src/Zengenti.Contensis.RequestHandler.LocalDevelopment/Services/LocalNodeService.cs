using Newtonsoft.Json.Linq;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;
using Zengenti.Rest.RestClient;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public class LocalNodeService : INodeService
{
    private readonly ISiteConfigLoader _siteConfigLoader;
    private SiteConfig _siteConfig;
    private readonly RestClient _internalRestClient;
    private readonly ILogger<LocalNodeService> _logger;

    private SiteConfig SiteConfig
    {
        get
        {
            if (!_siteConfig.Equals(_siteConfigLoader.SiteConfig))
            {
                _siteConfig = _siteConfigLoader.SiteConfig;
            }

            return _siteConfig;
        }
    }

    public LocalNodeService(
        ISiteConfigLoader siteConfigLoader,
        ISecurityTokenProviderFactory securityTokenProviderFactory,
        ILogger<LocalNodeService> logger)
    {
        _logger = logger;
        _siteConfigLoader = siteConfigLoader;
        _siteConfig = _siteConfigLoader.SiteConfig;

        var securityTokenParams =
            new SecurityTokenParams(
                _siteConfig.Alias,
                _siteConfig.ClientId,
                _siteConfig.SharedSecret,
                _siteConfig.Username,
                _siteConfig.Password);

        var securityTokenProvider = securityTokenProviderFactory.GetSecurityTokenProvider(securityTokenParams);
        _internalRestClient =
            new RestClientFactory($"https://cms-{securityTokenParams.Alias}.cloud.contensis.com/")
                .SecuredRestClient(securityTokenProvider);
    }

    public async Task<Node?> GetByPath(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && SiteConfig.Nodes.Count > 0)
            {
                var siteConfigNode = SiteConfig.GetNodePyPath(path);
                if (siteConfigNode != null)
                {
                    return siteConfigNode;
                }
            }

            _internalRestClient.AddHeader(Constants.Headers.IsLocalRequestHandler, "true");
            var requestUrl =
                $"api/management/projects/{_siteConfigLoader.SiteConfig.ProjectApiId}/nodes/{path.Trim('/')}";
            var restManagementNode = (await _internalRestClient.GetAsync<JObject>(requestUrl)).ResponseObject;

            if (restManagementNode == null)
            {
                _logger.LogWarning("Could not find a delivery node for path {Path}", path);
                return null;
            }

            Guid? rendererUuid = null;
            var rendererId = "";
            var isPartialMatchRoot = false;
            var rendererNode = GetObject(restManagementNode, "renderer");
            if (rendererNode != null)
            {
                rendererUuid = GetGuid(rendererNode, "id");
                isPartialMatchRoot = GetBool(rendererNode, "isPartialMatchRoot") ?? false;
            }

            if (rendererUuid != null)
            {
                var renderer = (await _internalRestClient.GetAsync<JObject>(
                        $"api/management/projects/{_siteConfigLoader.SiteConfig.ProjectApiId}/renderers/{rendererUuid}"))
                    .ResponseObject;
                rendererId = renderer != null ? GetString(renderer, "id") ?? "" : "";
            }

            var pathNode = GetObject(restManagementNode, "path");
            var node = new Node
            {
                Id = GetGuid(restManagementNode, "id"),
                Path = (pathNode != null ? GetString(pathNode, "en-GB") : null) ?? string.Empty,
                EntryId = GetGuid(restManagementNode, "entryId"),
                ContentTypeId = GetGuid(restManagementNode, "contentTypeId")
            };

            if (!string.IsNullOrWhiteSpace(rendererId) && rendererUuid != null)
            {
                node.RendererRef = new RendererRef
                {
                    Id = rendererId,
                    Uuid = rendererUuid.Value,
                    IsPartialMatchRoot = isPartialMatchRoot
                };
            }

            return node;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting a management node or renderer for path {Path}", path);
            return null;
        }
    }

    private static JObject? GetObject(JObject obj, string propertyName)
    {
        return obj.TryGetValue(propertyName, out var node) ? node as JObject : null;
    }

    private static string? GetString(JObject obj, string propertyName)
    {
        if (!obj.TryGetValue(propertyName, out var node) || node.Type == JTokenType.Null)
        {
            return null;
        }

        return node.Type == JTokenType.String ? node.Value<string>() : node.ToString();
    }

    private static bool? GetBool(JObject obj, string propertyName)
    {
        if (!obj.TryGetValue(propertyName, out var node) || node.Type == JTokenType.Null)
        {
            return null;
        }

        if (node.Type == JTokenType.Boolean)
        {
            return node.Value<bool>();
        }

        var text = node.ToString();
        return bool.TryParse(text, out var result) ? result : null;
    }

    private static Guid? GetGuid(JObject obj, string propertyName)
    {
        var value = GetString(obj, propertyName);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}