using Newtonsoft.Json.Linq;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Blocks;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;
using Zengenti.Rest.RestClient;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;

public class HttpPublishingApi : IPublishingApi
{
    private readonly SiteConfigLoader _siteConfigLoader;
    private readonly RestClient _internalRestClient;
    private SiteConfig _siteConfig;

    private SiteConfig SiteConfig
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

    public HttpPublishingApi(SiteConfigLoader siteConfigLoader)
    {
        _siteConfigLoader = siteConfigLoader;
        var securityTokenParams =
            new SecurityTokenParams(SiteConfig.Alias, SiteConfig.ClientId, SiteConfig.SharedSecret);

        _internalRestClient =
            new RestClientFactory($"https://cms-{securityTokenParams.Alias}.cloud.contensis.com/")
                .SecuredRestClient(new InternalSecurityTokenProvider(securityTokenParams));

        // Only used for debugging locally
        // _internalRestClient =
        //     new RestClientFactory($"http://localhost:5000/")
        //         .SecuredRestClient(new InternalSecurityTokenProvider(securityTokenParams));
        // _internalRestClient.AddHeader("x-alias", securityTokenParams.Alias);
    }

    public async Task<BlockVersionInfo?> GetBlockVersionInfo(Guid versionId)
    {
        var blockVersion = (await _internalRestClient.GetAsync<dynamic>(
                $"api/management/projects/{_siteConfigLoader.SiteConfig.ProjectId}/blocks/versions/{versionId}"))
            .ResponseObject;

        if (blockVersion == null)
        {
            return null;
        }

        var projectUuid = Guid.Empty; // NOT required for local development ATM.

        // TODO: check if we need to populate enableFullUriRouting
        var enableFullUriRouting = false;
        var blockVersionInfo = new BlockVersionInfo(projectUuid, "", versionId, new Uri(""), "", enableFullUriRouting,
            new[] { "" }, 1);
        return blockVersionInfo;
    }

    public async Task<EndpointRequestInfo?> GetEndpointForRequest(RequestContext requestContext)
    {
        var httpEndpointRequestContext = new HttpEndpointRequestContext(requestContext);
        var endpointRequestInfo = (await _internalRestClient.PostAsJsonAsync<dynamic>(
                $"api/management/projects/{_siteConfigLoader.SiteConfig.ProjectId}/renderers/endpointrequestinfo",
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
            ((JToken)endpointRequestInfo["staticPaths"]).ToObject<string[]>().ToList(),
            Guid.Parse(endpointRequestInfo["rendererId"].ToString()),
            null,
            endpointRequestInfo["branch"].ToString(),
            (int)endpointRequestInfo["blockVersionNo"],
            enableFullUriRouting,
            null);
    }

    public Task<IList<BlockWithVersions>> ListBlocksThatAreAvailable(Guid projectUuid, string viewType,
        string activeBlockVersionConfig,
        string defaultBlockVersionConfig, string serverType)
    {
        throw new NotImplementedException();
    }
}