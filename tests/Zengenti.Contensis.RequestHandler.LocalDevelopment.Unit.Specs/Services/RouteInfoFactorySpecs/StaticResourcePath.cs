using System.Web;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteInfoFactorySpecs;

public class StaticResourcePath
{
    private readonly IRouteInfoFactory _sut =
        new RouteInfoFactory(SpecHelper.CreateRequestContext(), new AppConfiguration());

    private RouteInfo _result;
    private readonly string _baseUriString = "http://my-block.contensis.com";

    public void GivenAStaticResourcePathContainsABlockIdPrefix()
    {
    }

    public void WhenCreateIsInvoked()
    {
        var blockGuid = Guid.NewGuid();
        var projectUuid = Guid.NewGuid();
        _result = _sut.CreateForNonNodePath(
            new Uri(
                $"http://www.mysite.com/_{RouteInfo.GetUrlFriendlyHash(Guid.NewGuid())}_{Guid.NewGuid()}/images/header.png?w=100&h=200"),
            new Headers(
                new Dictionary<string, string>
                {
                    {
                        Constants.Headers.Alias, "zenhub"
                    }
                }),
            new BlockVersionInfo(projectUuid, "", blockGuid, new Uri(_baseUriString), "main", false, null, 1));
    }

    public void ThenTheUriIsRewrittenCorrectly()
    {
        Assert.That(_result, Is.Not.Null);
        Assert.That(_result.Uri.ToString(), Is.EqualTo(_baseUriString + "/images/header.png?w=100&h=200"));
    }

    public void AndThenTheHeadersAreMapped()
    {
        Assert.That(_result.Headers.GetFirstValueIfExists(Constants.Headers.Alias) == "zenhub");
    }

    public void AndThenTheQueryStringValuesAreCorrect()
    {
        var query = HttpUtility.ParseQueryString(_result.Uri.Query);
        Assert.That(query.Count, Is.EqualTo(2));
        Assert.That(query["w"], Is.EqualTo("100"));
        Assert.That(query["h"], Is.EqualTo("200"));
    }

    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}