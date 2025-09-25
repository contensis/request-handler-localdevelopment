using Newtonsoft.Json.Linq;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Blocks;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;
using Zengenti.Rest.RestClient;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public class HttpPublishingApi : IPublishingApi
{
    private readonly ISiteConfigLoader _siteConfigLoader;
    private readonly RestClient _internalRestClient;
    private SiteConfig _siteConfig;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<HttpPublishingApi> _logger;

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

    public HttpPublishingApi(
        IRequestContext requestContext,
        ISiteConfigLoader siteConfigLoader,
        ISecurityTokenProviderFactory securityTokenProviderFactory,
        ILogger<HttpPublishingApi> logger)
    {
        _logger = logger;
        _requestContext = requestContext;
        _siteConfigLoader = siteConfigLoader;
        _siteConfig = _siteConfigLoader.SiteConfig;

        var securityTokenParams =
            new SecurityTokenParams(
                SiteConfig.Alias,
                SiteConfig.ClientId,
                SiteConfig.SharedSecret,
                SiteConfig.Username,
                SiteConfig.Password);
        var securityTokenProvider = securityTokenProviderFactory.GetSecurityTokenProvider(securityTokenParams);

        _internalRestClient =
            new RestClientFactory($"https://cms-{securityTokenParams.Alias}.cloud.contensis.com/")
                .SecuredRestClient(securityTokenProvider);

        // Only used for debugging locally
        // _internalRestClient =
        //     new RestClientFactory($"http://localhost:5000/")
        //         .SecuredRestClient(new InternalSecurityTokenProvider(securityTokenParams));
        // _internalRestClient.AddHeader("x-alias", securityTokenParams.Alias);
    }

    public async Task<BlockVersionInfo?> GetBlockVersionInfo(Guid versionId)
    {
        var blockVersion = (await _internalRestClient.GetAsync<dynamic>(
                $"api/management/projects/{_requestContext.ProjectApiId}/blocks/versions/{versionId}"))
            .ResponseObject;

        if (blockVersion == null)
        {
            _logger.LogWarning("Could not find block version with uuid {Uuid} using the http api", versionId);
            return null;
        }

        var blockId = (string)blockVersion.id;
        var block = _siteConfig.GetBlockById(blockId);
        if (block == null)
        {
            _logger.LogWarning("Could not find block version with id {Id} in site config", blockId);
            return null;
        }

        var blockVersionInfo = new BlockVersionInfo(
            _requestContext.ProjectUuid,
            blockId,
            versionId,
            block.BaseUri!,
            block.Branch,
            block.EnableFullUriRouting ?? false,
            block.Pushed.Value,
            block.StaticPaths,
            block.VersionNo);
        return blockVersionInfo;
    }

    public async Task<EndpointRequestInfo?> GetEndpointForRequest(RequestContext requestContext)
    {
        var httpEndpointRequestContext = new HttpEndpointRequestContext(requestContext);
        var endpointRequestInfo = (await _internalRestClient.PostAsJsonAsync<dynamic>(
                $"api/management/projects/{_requestContext.ProjectApiId}/renderers/endpointrequestinfo",
                httpEndpointRequestContext))
            .ResponseObject;
        var layoutRendererIdValue = endpointRequestInfo["layoutRendererId"]?.ToString();
        Guid? layoutRendererId = null;
        if (!String.IsNullOrWhiteSpace(layoutRendererIdValue))
        {
            layoutRendererId = Guid.Parse(layoutRendererIdValue);
        }

        // TODO: check if we need to populate enableFullUriRouting
        var enableFullUriRouting = false;
        return new EndpointRequestInfo(
            endpointRequestInfo["blockId"].ToString(),
            Guid.Parse(endpointRequestInfo["blockVersionId"].ToString()),
            endpointRequestInfo["endpointID"]?.ToString(),
            layoutRendererId,
            endpointRequestInfo["uri"].ToString(),
            ((JToken)endpointRequestInfo["staticPaths"])?.ToObject<string[]>()?.ToList(),
            Guid.Parse(endpointRequestInfo["rendererId"].ToString()),
            null,
            endpointRequestInfo["branch"].ToString(),
            (int)endpointRequestInfo["blockVersionNo"],
            enableFullUriRouting,
            DateTime.Parse(endpointRequestInfo["pushed"].ToString()),
            null);
    }

    public Task<IList<BlockWithVersions>> ListBlocksThatAreAvailable(
        Guid projectUuid,
        string viewType,
        string activeBlockVersionConfig,
        string defaultBlockVersionConfig,
        string serverType)
    {
        throw new NotImplementedException();
    }
}