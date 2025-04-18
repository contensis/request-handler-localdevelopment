﻿using System.Web;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteInfoFactorySpecs;

public class ProxyPath
{
    private readonly IRouteInfoFactory _sut =
        new RouteInfoFactory(SpecHelper.CreateRequestContext(), new AppConfiguration());

    private RouteInfo _result;
    private readonly Uri _baseUri = new("https://www.legacysite.com");

    public void GivenAnOriginPathIsAProxyPath()
    {
        // Empty
    }

    public void WhenCreateIsInvoked()
    {
        _result = _sut.Create(
            _baseUri,
            new Uri("http://www.mysite.com/news/woman-worlds-longest-nails?page=1"),
            new Headers(
                new Dictionary<string, string>
                {
                    {
                        Constants.Headers.Alias, "zenhub"
                    }
                }),
            nodeInfo: new NodeInfo(
                Guid.NewGuid(),
                null,
                ""),
            proxyInfo: new ProxyInfo(Guid.NewGuid(), false, false));
    }

    public void ThenTheUriIsRewrittenCorrectly()
    {
        Assert.That(_result, Is.Not.Null);
        Assert.That(_result.Uri.SiteRoot().TrimEnd('/'), Is.EqualTo(_baseUri.ToString().TrimEnd('/')));
        Assert.That(_result.Uri.AbsolutePath, Is.EqualTo("/news/woman-worlds-longest-nails"));
    }

    public void AndThenTheHeadersAreMapped()
    {
        Assert.That(_result.Headers.GetFirstValueIfExists(Constants.Headers.Alias) == "zenhub");
    }

    public void AndThenTheQueryStringValuesAreCorrect()
    {
        var query = HttpUtility.ParseQueryString(_result.Uri.Query);
        Assert.That(query.Count, Is.EqualTo(2));
        Assert.That(query["page"], Is.EqualTo("1"));
    }

    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}