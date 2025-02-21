using Microsoft.Extensions.Logging;
using NSubstitute;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteServiceSpecs;

public class PathIsAliasWithApiRouteCall
{
    private const string Host = "http://www.mysite.com";
    private const string Path = "/api/delivery/website/";
    private readonly Uri _originUri = new(Host + Path);
    private readonly Headers _headers = new();

    private RouteService _sut;
    private INodeService _nodeService;
    private RouteInfo _result;

    public void GivenARequestPathIsAnApiCall()
    {
        _nodeService = Substitute.For<INodeService>();

        var logger = Substitute.For<ILogger<RouteService>>();
        var requestContext = SpecHelper.CreateRequestContext();
        var cacheKeyService = Substitute.For<ICacheKeyService>();
        var blockClusterConfig = new AppConfiguration(
            AliasesWithApiRoutes: new[]
            {
                "test"
            });
        var routeInfoFactory = new RouteInfoFactory(
            requestContext,
            blockClusterConfig);
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

    public async Task WhenTheRouteIsRequested()
    {
        _result = await _sut.GetRouteForRequest(_originUri, _headers);
    }

    public void ThenNodeLookupIsPerformed()
    {
        _nodeService.ReceivedWithAnyArgs().GetByPath(Path);
    }

    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}