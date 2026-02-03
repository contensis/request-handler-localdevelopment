using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Extensions;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

public class SiteConfig
{
    public List<ContentTypeRendererMapItem> ContentTypeRendererMap { get; } =
        new List<ContentTypeRendererMapItem>();

    public string Alias { get; set; } = null!;

    public string ProjectApiId { get; set; } = null!;

    public string? IisHostname { get; set; }

    public string? PodIngressIp { get; set; }

    public string? AccessToken { get; set; }
    public string? ClientId { get; set; }

    public string? SharedSecret { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public List<Node> Nodes { get; set; } = new();

    public List<Renderer> Renderers { get; set; } = [];

    public List<Proxy> Proxies { get; set; } = [];

    public List<Block> Blocks { get; set; } = [];

    public Block? GetBlockById(string id)
    {
        return Blocks.FirstOrDefault(b => b.Id.EqualsCaseInsensitive(id));
    }

    public Node? GetNodePyPath(string path)
    {
        var node = Nodes.FirstOrDefault(n => n.Path.StartsWith(path));
        if (node != null)
        {
            return node;
        }

        return Nodes.FirstOrDefault(n => n.Path == "/");
    }

    public Block? GetBlockByUuid(Guid id)
    {
        return Blocks.FirstOrDefault(b => b.Uuid == id);
    }

    public Renderer? GetRendererById(string id)
    {
        return Renderers.FirstOrDefault(r => r.Id.EqualsCaseInsensitive(id));
    }

    public Renderer? GetRendererByContentTypeUuid(Guid? contentTypeUuid)
    {
        var item = ContentTypeRendererMap.FirstOrDefault(i => i.ContentTypeUuid == contentTypeUuid);
        if (item != null)
        {
            return GetRendererByUuid(item.RendererUuid);
        }

        return null;
    }

    public Renderer? GetRendererByUuid(Guid uuid)
    {
        return Renderers.FirstOrDefault(r => r.Uuid == uuid);
    }

    public Proxy? GetProxyByUuid(Guid uuid)
    {
        return Proxies.FirstOrDefault(r => r.Id == uuid);
    }

    public static SiteConfig? LoadFromFile(string configPath)
    {
        if (File.Exists(configPath))
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new UriTypeConverter())
                .Build();

            var siteConfig = deserializer.Deserialize<SiteConfig>(File.ReadAllText(configPath));

            ResolveReferences(siteConfig);

            return siteConfig;
        }

        return null;
    }

    public static SiteConfig LoadFromJson(
        string alias,
        string projectApiId,
        string blocksAsJson,
        string? renderersAsJson = null,
        string? accessToken = null,
        string? clientId = null,
        string? sharedSecret = null,
        string? username = null,
        string? password = null,
        string? iisHostname = null,
        string? podIngressIp = null)
    {
        var siteConfig = new SiteConfig
        {
            Alias = alias,
            ProjectApiId = projectApiId,
            AccessToken = accessToken,
            ClientId = clientId,
            SharedSecret = sharedSecret,
            Username = username,
            Password = password,
            IisHostname = iisHostname,
            PodIngressIp = podIngressIp
        };

        siteConfig.Blocks = JsonSerializer.Deserialize(blocksAsJson, AppJsonSerializerContext.Default.ListBlock)!;

        if (!string.IsNullOrWhiteSpace(renderersAsJson))
        {
            siteConfig.Renderers = JsonSerializer.Deserialize(
                renderersAsJson,
                AppJsonSerializerContext.Default.ListRenderer)!;
        }

        ResolveReferences(siteConfig);

        return siteConfig;
    }

    private static void ResolveReferences(SiteConfig siteConfig)
    {
        foreach (var block in siteConfig.Blocks)
        {
            block.EnsureDefaultStaticPaths();
            foreach (var endpoint in block.Endpoints)
            {
                endpoint.Block = block;
            }
        }

        foreach (var renderer in siteConfig.Renderers)
        {
            foreach (var rule in renderer.Rules)
            {
                rule.Return!.BlockUuid = siteConfig.GetBlockById(rule.Return.BlockId!)!.Uuid;
            }

            foreach (var contentType in renderer.AssignedContentTypes)
            {
                var mapItem = new ContentTypeRendererMapItem
                {
                    ContentTypeId = contentType,
                    ContentTypeUuid = Guid.NewGuid(),
                    RendererId = renderer.Id,
                    RendererUuid = renderer.Uuid!.Value
                };

                siteConfig.ContentTypeRendererMap.Add(mapItem);
            }

            if (!string.IsNullOrWhiteSpace(renderer.LayoutRenderer))
            {
                renderer.LayoutRendererId = siteConfig.GetRendererById(renderer.LayoutRenderer)?.Uuid;
            }
        }
    }
}