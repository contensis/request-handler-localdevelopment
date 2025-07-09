using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteInfoFactorySpecs;

public class FullUriRoutingWithTrailingSlashForApiPath
{
    private readonly IRouteInfoFactory _sut =
        new RouteInfoFactory(
            SpecHelper.CreateRequestContext(),
            new AppConfiguration("block-cluster", "10.0.0.1", "block_suffix"));

    private RouteInfo _result;
    private string _baseUrl;
    private readonly Guid _nodeId = Guid.NewGuid();
    private readonly Guid _entryId = Guid.NewGuid();
    private readonly string _path = "/api/some-api-path/";
    private readonly string _queryString = "?page=2";
    private readonly string _originalBaseUrl = "http://www.mysite.com";
    private string _expectedBaseUrl;

    public void GivenABaseUriWithNoEndpointPath()
    {
    }

    public void WhenCreateIsInvoked()
    {
        _result = _sut.Create(
            new Uri(_baseUrl),
            new Uri($"{_originalBaseUrl}{_path}{_queryString}"),
            new Headers(
                new Dictionary<string, string>
                {
                    {
                        Constants.Headers.Alias, "zenhub"
                    }
                }),
            new NodeInfo(_nodeId, _entryId, ""),
            new BlockVersionInfo(
                Guid.NewGuid(),
                "",
                Guid.NewGuid(),
                new Uri(_baseUrl),
                "main",
                true,
                null,
                1));
    }

    public void ThenTheUriIsRewrittenCorrectly()
    {
        Assert.That(_result, Is.Not.Null);
        Assert.That(
            _result.Uri!.ToString(),
            Is.EqualTo($"{_expectedBaseUrl}{_path}{_queryString}&nodeId={_nodeId}&entryId={_entryId}"));
    }

    public void AndThenTheHeadersAreMapped()
    {
        Assert.That(_result.Headers.GetFirstValueIfExists(Constants.Headers.Alias) == "zenhub");
    }

    public void AndThenTheBlockClusterDetailsAreSet()
    {
        var expectedHost = new Uri(_baseUrl).Host;
        Assert.That(_result.Headers.GetFirstValueIfExists("host"), Is.EqualTo(expectedHost));
    }

    [TestCase("http://my-block.contensis.com", "http://10.0.0.1")]
    [TestCase("https://my-block.contensis.com/", "https://10.0.0.1")]
    [TestCase("http://my-block.contensis.com:5001", "http://10.0.0.1:5001")]
    public void Run(string baseUrl, string expectedBaseUrl)
    {
        _baseUrl = baseUrl;
        _expectedBaseUrl = expectedBaseUrl;
        this.BDDfy();
    }
}