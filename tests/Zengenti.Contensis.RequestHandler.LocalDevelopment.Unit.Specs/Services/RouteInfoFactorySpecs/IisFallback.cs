using System.Web;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteInfoFactorySpecs;

public class IisFallback
{
    private readonly IRouteInfoFactory _sut =
        new RouteInfoFactory(
            SpecHelper.CreateRequestContext(), 
            new BlockClusterConfig("10.0.1.2", "suffix"));

    private RouteInfo _result;

    [Given]
    public void GivenAnOriginPathIsAnIisFallbackUri()
    {
    }

    [When]
    public void WhenCreateIsInvoked()
    {
        _result = _sut.CreateForIisFallback(
            new Uri("http://www.mysite.com/news/today-is-the-day?page=2"),
            new Headers(new Dictionary<string, string>
            {
                { Constants.Headers.Alias, "zenhub" },
                { Constants.Headers.LoadBalancerVip, "10.0.0.1" },
                { Constants.Headers.IisHostName, "www.mysite.com" }
            }));
    }

    [Then]
    public void ThenTheUriIsRewrittenCorrectly()
    {
        Assert.That(_result, Is.Not.Null);
        Assert.That(_result.Uri.SiteRoot().TrimEnd('/'), Is.EqualTo("https://10.0.0.1"));
        Assert.That(_result.Uri.AbsolutePath, Is.EqualTo("/news/today-is-the-day"));
    }

    [AndThen]
    public void AndThenTheHeadersAreMapped()
    {
        Assert.That(_result.Headers.Values.Count, Is.EqualTo(4));
        Assert.That(_result.Headers.GetFirstValueIfExists(Constants.Headers.Alias) == "zenhub");
    }
    
    [AndThen]
    public void AndThenTheHostHeaderIsSetToTheOriginHeader()
    {
        Assert.That(_result.Headers.GetFirstValueIfExists("host"), Is.EqualTo("www.mysite.com"));
    }

    [AndThen]
    public void AndThenTheQueryStringValuesAreCorrect()
    {
        var query = HttpUtility.ParseQueryString(_result.Uri.Query);
        
        Assert.That(query.Count, Is.EqualTo(1));
        Assert.That(query["page"], Is.EqualTo("2"));
    }
    
    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}