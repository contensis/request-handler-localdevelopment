using Microsoft.Extensions.Logging;
using NSubstitute;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteServiceSpecs;

public class NodeDoesNotExistAndRouteIsNotProxied
{
    private const string Host = "http://www.mysite.com";
    private const string Path = "/blogs/keeping-it-real";
    private RouteService _sut;
    private INodeService _nodeService;
    private RouteInfo _result;

    [Given]
    public void GivenARequestPathDoesNotExistsAsANode()
    {
        _nodeService = Substitute.For<INodeService>();
        _nodeService.GetByPath(Path).Returns((Node)null);

        var requestContext = Substitute.For<IRequestContext>();
        var cacheKeyService = Substitute.For<ICacheKeyService>();
        var logger = Substitute.For<ILogger<RouteService>>();
        var routeInfoFactory = new RouteInfoFactory(requestContext, new BlockClusterConfig());
        var rendererService = SpecHelper.CreatePublishingService(routeInfoFactory);

        _sut = new RouteService(
            _nodeService,
            rendererService,
            routeInfoFactory,
            requestContext,
            cacheKeyService,
            logger);
    }

    [When]
    public async Task WhenTheRouteIsRequested()
    {
        _result = await _sut.GetRouteForRequest(new Uri(Host + Path), new Headers());
    }

    [Then]
    public void ThenTheNodeIsLookedUp()
    {
        _nodeService.Received(1).GetByPath(Path);
    }

    [AndThen]
    public void AndThenNoRouteIsReturned()
    {
        Assert.That(_result.FoundRoute, Is.EqualTo(false));
    }

    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}