using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.LocalDevelopment;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs;

namespace Zengenti.Contensis.RequestHandler.Unit.Specs.Config;

public class SiteConfigLoaderSpecs
{
    private SiteConfigLoader _siteConfigLoaderFromFile;
    private SiteConfigLoader _siteConfigLoaderFromJson;

    public void GivenAYamlConfigAndAJsonConfigThatAreIdentical()
    {
    }

    public void WhenTheSiteConfigAreLoaded()
    {
        _siteConfigLoaderFromFile = new SiteConfigLoader("Config/site_config.yaml");

        _siteConfigLoaderFromJson = new SiteConfigLoader("test", "website", SpecHelper.GetFile("Config/blocks.json"),
            SpecHelper.GetFile("Config/renderers.json"), "token1", "client1", "secret1");
    }

    public async Task ThenTheyAreEqual()
    {
        await Verify(new
        {
            YamlSiteConfig = _siteConfigLoaderFromFile.SiteConfig,
            JsonSiteConfig = _siteConfigLoaderFromJson.SiteConfig
        });
    }
    
    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}