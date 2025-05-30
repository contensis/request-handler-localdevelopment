﻿using Microsoft.Extensions.Logging;
using NSubstitute;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteServiceSpecs;

public class InvalidPathExists
{
    private const string Host = "http://www.mysite.com";
    private const string Path = "/blogs/keeping-it-real";
    private RouteService _sut;
    private INodeService _nodeService;
    private RouteInfo _result;

    public void GivenARequestPathExistsAsANode()
    {
        var node = new Node
        {
            Path = Path,
            Id = Guid.NewGuid(),
            EntryId = Guid.NewGuid()
        };

        _nodeService = Substitute.For<INodeService>();
        _nodeService.GetByPath(Path).Returns(node);

        var requestContext = Substitute.For<IRequestContext>();
        var cacheKeyService = Substitute.For<ICacheKeyService>();
        var logger = Substitute.For<ILogger<RouteService>>();
        var blockClusterConfig = new AppConfiguration();
        var routeInfoFactory =
            new RouteInfoFactory(requestContext, blockClusterConfig);
        var publishingService = SpecHelper.CreatePublishingService(routeInfoFactory);

        _sut = new RouteService(
            blockClusterConfig,
            _nodeService,
            publishingService,
            routeInfoFactory,
            requestContext,
            cacheKeyService,
            logger);
    }

    public void AndGivenThereIsNoMatchingRendererForTheRequest()
    {
    }

    public async Task WhenTheRouteIsRequested()
    {
        _result = await _sut.GetRouteForRequest(new Uri(Host + Path), new Headers());
    }

    public void ThenTheNodeIsLookedUp()
    {
        _nodeService.Received(1).GetByPath(Path);
    }

    public void AndThenNoRouteIsReturned()
    {
        Assert.That(_result.RouteType, Is.EqualTo(RouteType.NotFound));
    }

    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}