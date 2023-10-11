using Zengenti.Contensis.Delivery;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;
using Zengenti.Rest.RestClient;
using Node = Zengenti.Contensis.RequestHandler.Domain.Entities.Node;

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

    public LocalNodeService(ISiteConfigLoader siteConfigLoader,
        ISecurityTokenProviderFactory securityTokenProviderFactory, ILogger<LocalNodeService> logger)
    {
        _logger = logger;
        _siteConfigLoader = siteConfigLoader;
        _siteConfig = _siteConfigLoader.SiteConfig!;

        var securityTokenParams =
            new SecurityTokenParams(_siteConfig!.Alias, _siteConfig.ClientId, _siteConfig.SharedSecret,
                _siteConfig.Username, _siteConfig.Password);

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

            var restManagementNode = (await _internalRestClient.GetAsync<dynamic>(
                    $"api/management/projects/{_siteConfigLoader.SiteConfig.ProjectId}/nodes/{path.Trim('/')}"))
                .ResponseObject;

            if (restManagementNode == null)
            {
                _logger.LogWarning("Could not find a delivery node for path {Path}", path);
                return null;
            }
            
            Guid? rendererUuid = null;
            string rendererId = null;
            var isPartialMatchRoot = false;
            if (restManagementNode["renderer"] != null)
            {
                var nodeRenderer = restManagementNode["renderer"];
                rendererUuid = Guid.Parse(nodeRenderer["id"].ToString());
                isPartialMatchRoot = bool.Parse(nodeRenderer["isPartialMatchRoot"].ToString());
            }

            if (rendererUuid != null)
            {
                var renderer = (await _internalRestClient.GetAsync<dynamic>(
                        $"api/management/projects/{_siteConfigLoader.SiteConfig.ProjectId}/renderers/{rendererUuid}"))
                    .ResponseObject;
                rendererId = renderer["id"].ToString();
            }

            var node = new Node
            {
                Id = restManagementNode["id"],
                Path = restManagementNode["path"]["en-GB"],
                EntryId = restManagementNode["entryId"],
            };

            if (!string.IsNullOrWhiteSpace(rendererId))
            {
                node.RendererRef = new RendererRef()
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
            _logger.LogError(e, "Error getting a management node or renderer");
            return null;
        }
    }
}