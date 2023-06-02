using Microsoft.Extensions.Logging;
using Zengenti.Contensis.Delivery;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Rest.RestClient;
using Node = Zengenti.Contensis.RequestHandler.Domain.Entities.Node;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public class LocalNodeService : INodeService
{
    private readonly SiteConfigLoader _siteConfigLoader;
    private ContensisClient? _deliveryClient;
    private readonly RestClient _internalRestClient;
    private SiteConfig? _siteConfig;
    private ILogger<LocalNodeService> _logger;

    public LocalNodeService(SiteConfigLoader siteConfigLoader, ILogger<LocalNodeService> logger)
    {
        _logger = logger;
        _siteConfigLoader = siteConfigLoader ?? throw new ArgumentNullException(nameof(siteConfigLoader));
        var securityTokenParams =
            new SecurityTokenParams(SiteConfig!.Alias, SiteConfig.ClientId, SiteConfig.SharedSecret);

        _internalRestClient =
            new RestClientFactory($"https://cms-{securityTokenParams.Alias}.cloud.contensis.com/")
                .SecuredRestClient(new InternalSecurityTokenProvider(securityTokenParams));
    }

    private ContensisClient DeliveryClient
    {
        get
        {
            if (_deliveryClient == null)
            {
                _deliveryClient = CreateDeliveryClient();
            }

            return _deliveryClient;
        }
    }

    private SiteConfig? SiteConfig
    {
        get
        {
            if (_siteConfig != _siteConfigLoader.SiteConfig)
            {
                _siteConfig = _siteConfigLoader.SiteConfig;
            }

            return _siteConfig;
        }
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

            var restDeliveryNode = await DeliveryClient.Nodes.GetByPathAsync(path);
            if (restDeliveryNode == null)
            {
                _logger.LogWarning($"Could not find a delivery node for path {path}.");
                return null;
            }

            var restManagementNode = (await _internalRestClient.GetAsync<dynamic>(
                    $"api/management/projects/{_siteConfigLoader.SiteConfig.ProjectId}/nodes/{restDeliveryNode.Id}"))
                .ResponseObject;

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
                Id = restManagementNode.Id,
                Path = restDeliveryNode.Path,
                EntryId = restDeliveryNode.EntryId,
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

    private ContensisClient CreateDeliveryClient()
    {
        if (_deliveryClient == null || _siteConfig != _siteConfigLoader.SiteConfig)
        {
            _deliveryClient = ContensisClient.Create(
                SiteConfig!.ProjectId,
                $"https://cms-{SiteConfig.Alias}.cloud.contensis.com",
                SiteConfig.ClientId,
                SiteConfig.SharedSecret,
                VersionStatus.Latest);
        }

        return _deliveryClient;
    }
}