using System.Web;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteInfoFactorySpecs;

public class FullUriRoutingAfterSiteViewQueryCutoffDate
{
    // Date used to test logic before the site view query cutoff.
    private static readonly DateTime SiteViewQueryCutoffDate = new DateTime(1990, 1, 1);

    private readonly IRouteInfoFactory _sut =
        new RouteInfoFactory(
            SpecHelper.CreateRequestContext(),
            new AppConfiguration(),
            SiteViewQueryCutoffDate);

    private RouteInfo _result;
    private string _baseUriString;
    private readonly Guid _nodeId = Guid.NewGuid();
    private readonly Guid _entryId = Guid.NewGuid();
    private readonly string _path = "/some-path";

    public void GivenABaseUriWithNoEndpointPath()
    {
    }

    public void WhenCreateIsInvoked()
    {
        _result = _sut.Create(
            new Uri(_baseUriString),
            new Uri($"http://www.mysite.com{_path}?page=2"),
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
                new Uri(_baseUriString),
                "main",
                true,
                new DateTime(2025, 1, 1),
                null,
                1));
    }

    public void ThenTheUriIsRewrittenCorrectly()
    {
        Assert.That(_result, Is.Not.Null);
        Assert.That(_result.Uri.SiteRoot().TrimEnd('/'), Is.EqualTo(new Uri(_baseUriString).SiteRoot().TrimEnd('/')));
        Assert.That(_result.Uri.AbsolutePath, Is.EqualTo(_path));
    }

    public void AndThenTheHeadersAreMapped()
    {
        Assert.That(_result.Headers.GetFirstValueIfExists(Constants.Headers.Alias) == "zenhub");
    }

    public void AndThenTheQueryStringValuesAreCorrect()
    {
        var query = HttpUtility.ParseQueryString(_result.Uri.Query);
        Assert.That(query.Count, Is.EqualTo(1));
        Assert.That(query["nodeId"], Is.Null);
        Assert.That(query["entryId"], Is.Null);
        Assert.That(query["page"], Is.EqualTo("2"));
    }

    [TestCase("http://my-block.contensis.com")]
    [TestCase("https://my-block.contensis.com/")]
    [TestCase("http://my-block.contensis.com:5001")]
    public void Run(string baseUriString)
    {
        _baseUriString = baseUriString;
        this.BDDfy();
    }
}