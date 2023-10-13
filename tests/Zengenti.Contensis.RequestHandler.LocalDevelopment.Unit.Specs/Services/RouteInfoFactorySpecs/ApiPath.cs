using System.Web;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteInfoFactorySpecs;

public class ApiPath
{
    private readonly IRouteInfoFactory _sut =
        new RouteInfoFactory(SpecHelper.CreateRequestContext(), new BlockClusterConfig());

    private RouteInfo _result;

    [Given]
    public void GivenAnOriginPathIsAnApiPath()
    {
        // Empty
    }

    [When]
    public void WhenCreateIsInvoked()
    {
        _result = _sut.CreateForNonNodePath(
            new Uri("http://www.mysite.com/api/delivery/projects/website/entries?versionStatus=latest"),
            new Headers(new Dictionary<string, string>
            {
                { Constants.Headers.Alias, "zenhub" }
            }));
    }

    [Then]
    public void ThenTheUriIsRewrittenCorrectly()
    {
        Assert.That(_result, Is.Not.Null);
        Assert.That(_result.Uri.SiteRoot().TrimEnd('/'), Is.EqualTo("https://api-test.cloud.contensis.com"));
        Assert.That(_result.Uri.AbsolutePath, Is.EqualTo("/api/delivery/projects/website/entries"));
    }

    [AndThen]
    public void AndThenTheHeadersAreMapped()
    {
        Assert.That(_result.Headers.GetFirstValueIfExists(Constants.Headers.Alias) == "zenhub");
    }
    
    [AndThen]
    public void AndThenTheQueryStringValuesAreCorrect()
    {
        var query = HttpUtility.ParseQueryString(_result.Uri.Query);
        Assert.That(query.Count, Is.EqualTo(1));
        Assert.That(query["versionStatus"], Is.EqualTo("latest"));
    }
    
    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}