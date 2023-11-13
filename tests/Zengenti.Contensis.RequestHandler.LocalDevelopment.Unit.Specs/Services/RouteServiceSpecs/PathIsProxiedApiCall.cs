using Microsoft.Extensions.Logging;
using NSubstitute;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteServiceSpecs;

public class PathIsProxiedApiCall
{
    private const string Host = "http://www.mysite.com";
    private const string Path = "/api/delivery/website/";
    private readonly Uri _originUri = new(Host + Path);
    private readonly Headers _headers = new();

    const string ApiUriString = "https://api-test.cloud.contensis.com";

    private RouteService _sut;
    private INodeService _nodeService;
    private RouteInfo _result;

    [Given]
    public void GivenARequestPathIsAnApiCall()
    {
        _nodeService = Substitute.For<INodeService>();

        var logger = Substitute.For<ILogger<RouteService>>();
        var requestContext = SpecHelper.CreateRequestContext();
        var cacheKeyService = Substitute.For<ICacheKeyService>();
        var routeInfoFactory = new RouteInfoFactory(requestContext, new BlockClusterConfig());
        var publishingService = SpecHelper.CreatePublishingService(routeInfoFactory);

        _sut = new RouteService(
            _nodeService,
            publishingService,
            routeInfoFactory,
            requestContext,
            cacheKeyService,
            logger);
    }

    [When]
    public async Task WhenTheRouteIsRequested()
    {
        _result = await _sut.GetRouteForRequest(_originUri, _headers);
    }

    [Then]
    public void ThenNoNodeLookupIsPerformed()
    {
        _nodeService.DidNotReceive().GetByPath(Path);
    }

    [AndThen]
    public void AndThenTheProxiedUriIsInvoked()
    {
        Assert.That(_result.Uri.ToString(), Is.EqualTo(ApiUriString + Path));
    }

    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}