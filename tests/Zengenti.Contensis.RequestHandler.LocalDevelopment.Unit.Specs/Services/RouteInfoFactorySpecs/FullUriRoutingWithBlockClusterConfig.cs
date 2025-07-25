﻿using System.Web;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteInfoFactorySpecs;

public class FullUriRoutingWithBlockClusterConfig
{
    private readonly IRouteInfoFactory _sut =
        new RouteInfoFactory(
            SpecHelper.CreateRequestContext(),
            new AppConfiguration("block-cluster", "10.0.0.1", "block_suffix"));

    private RouteInfo _result;
    private string _baseUriString;
    private string _expectedBaseUriString;

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
            new NodeInfo(
                _nodeId,
                _entryId,
                ""),
            new BlockVersionInfo(
                Guid.NewGuid(),
                "",
                Guid.NewGuid(),
                new Uri(_baseUriString),
                "main",
                true,
                null,
                1));
    }

    public void ThenTheUriIsRewrittenCorrectly()
    {
        Assert.That(_result, Is.Not.Null);
        Assert.That(_result.Uri.SiteRoot().TrimEnd('/'), Is.EqualTo(_expectedBaseUriString));
        Assert.That(_result.Uri.AbsolutePath, Is.EqualTo(_path));
    }

    public void AndThenTheHeadersAreMapped()
    {
        Assert.That(_result.Headers.GetFirstValueIfExists(Constants.Headers.Alias) == "zenhub");
    }

    public void AndThenTheBlockClusterDetailsAreSet()
    {
        var expectedHost = new Uri(_baseUriString).Host;
        Assert.That(_result.Headers.GetFirstValueIfExists("host"), Is.EqualTo(expectedHost));
    }

    public void AndThenTheQueryStringValuesAreCorrect()
    {
        var query = HttpUtility.ParseQueryString(_result.Uri.Query);
        Assert.That(query.Count, Is.EqualTo(3));
        Assert.That(query["nodeId"], Is.EqualTo(_nodeId.ToString()));
        Assert.That(query["entryId"], Is.EqualTo(_entryId.ToString()));
        Assert.That(query["page"], Is.EqualTo("2"));
    }

    [TestCase("http://my-block.contensis.com", "http://10.0.0.1")]
    [TestCase("https://my-block.contensis.com/", "https://10.0.0.1")]
    [TestCase("http://my-block.contensis.com:5001", "http://10.0.0.1:5001")]
    public void Run(string baseUriString, string expectedBaseUriString)
    {
        _baseUriString = baseUriString;
        _expectedBaseUriString = expectedBaseUriString;
        this.BDDfy();
    }
}