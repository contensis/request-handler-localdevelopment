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
        var blockVersion = (await _internalRestClient.GetAsync<JObject>(
                $"api/management/projects/{_requestContext.ProjectApiId}/blocks/versions/{versionId}"))
            .ResponseObject;

        if (blockVersion == null)
        {
            _logger.LogWarning("Could not find block version with uuid {Uuid} using the http api", versionId);
            return null;
        }

        var blockId = GetString(blockVersion, "id");
        if (string.IsNullOrWhiteSpace(blockId))
        {
            _logger.LogWarning("Block version response missing id for uuid {Uuid}", versionId);
            return null;
        }

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
            block.Pushed ?? Block.DefaultPushedDate,
            block.StaticPaths,
            block.VersionNo);
        return blockVersionInfo;
    }

    public async Task<EndpointRequestInfo?> GetEndpointForRequest(RequestContext requestContext)
    {
        var httpEndpointRequestContext = new HttpEndpointRequestContext(requestContext);
        var endpointRequestInfo = (await _internalRestClient.PostAsJsonAsync<JObject>(
                $"api/management/projects/{_requestContext.ProjectApiId}/renderers/endpointrequestinfo",
                httpEndpointRequestContext))
            .ResponseObject;

        if (endpointRequestInfo == null)
        {
            _logger.LogWarning("Endpoint request info not returned for {ProjectApiId}", _requestContext.ProjectApiId);
            return null;
        }

        var layoutRendererIdValue = GetString(endpointRequestInfo, "layoutRendererId");
        Guid? layoutRendererId = null;
        if (!string.IsNullOrWhiteSpace(layoutRendererIdValue))
        {
            layoutRendererId = Guid.Parse(layoutRendererIdValue);
        }

        // TODO: check if we need to populate enableFullUriRouting
        var enableFullUriRouting = false;
        return new EndpointRequestInfo(
            GetRequiredString(endpointRequestInfo, "blockId"),
            GetRequiredGuid(endpointRequestInfo, "blockVersionId"),
            GetString(endpointRequestInfo, "endpointID") ?? string.Empty,
            layoutRendererId,
            GetRequiredString(endpointRequestInfo, "uri"),
            GetStringList(endpointRequestInfo, "staticPaths") ?? [],
            GetRequiredGuid(endpointRequestInfo, "rendererId"),
            [],
            GetRequiredString(endpointRequestInfo, "branch"),
            GetRequiredInt(endpointRequestInfo, "blockVersionNo"),
            enableFullUriRouting,
            GetRequiredDateTime(endpointRequestInfo, "pushed"),
            []);
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

    private static string? GetString(JObject obj, string propertyName)
    {
        if (!obj.TryGetValue(propertyName, out var node) || node.Type == JTokenType.Null)
        {
            return null;
        }

        return node.Type == JTokenType.String ? node.Value<string>() : node.ToString();
    }

    private static string GetRequiredString(JObject obj, string propertyName)
    {
        var value = GetString(obj, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing or empty '{propertyName}' in response payload.");
        }

        return value;
    }

    private static Guid GetRequiredGuid(JObject obj, string propertyName)
    {
        var value = GetRequiredString(obj, propertyName);
        return Guid.Parse(value);
    }

    private static int GetRequiredInt(JObject obj, string propertyName)
    {
        if (obj.TryGetValue(propertyName, out var node) && node.Type != JTokenType.Null)
        {
            if (node.Type == JTokenType.Integer)
            {
                return node.Value<int>();
            }

            var text = node.ToString();
            if (int.TryParse(text, out var number))
            {
                return number;
            }
        }

        throw new InvalidOperationException($"Missing or invalid '{propertyName}' in response payload.");
    }

    private static DateTime GetRequiredDateTime(JObject obj, string propertyName)
    {
        var value = GetRequiredString(obj, propertyName);
        return DateTime.Parse(value);
    }

    private static List<string>? GetStringList(JObject obj, string propertyName)
    {
        if (!obj.TryGetValue(propertyName, out var node) || node is not JArray array)
        {
            return null;
        }

        return array
            .Select(item => item.Type == JTokenType.String ? item.Value<string?>() : item.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
    }
}