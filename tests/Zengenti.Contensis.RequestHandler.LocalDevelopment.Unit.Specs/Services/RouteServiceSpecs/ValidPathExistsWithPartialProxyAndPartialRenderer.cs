using Microsoft.Extensions.Logging;
using NSubstitute;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;
using Zengenti.Contensis.RequestHandler.LocalDevelopment.Services.Interfaces;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteServiceSpecs;

public class ValidPathExistsWithPartialProxyAndPartialRenderer
{
    private const string Host = "http://www.mysite.com";
    private const string Path = "/blogs/keeping-it-real";
    private RouteService _sut;
    private INodeService _nodeService;
    private RouteInfo _result;
    private Node _node;
    private ILocalDevPublishingService _publishingService;
    private readonly Guid _projectUuid = Guid.NewGuid();

    public void GivenARequestPathExistsAsANode()
    {
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.ProjectUuid.Returns(_projectUuid);
        var cacheKeyService = Substitute.For<ICacheKeyService>();
        var logger = Substitute.For<ILogger<RouteService>>();
        var blockClusterConfig = new AppConfiguration();
        var routeInfoFactory = new RouteInfoFactory(requestContext, blockClusterConfig);
        _publishingService = SpecHelper.CreatePublishingService(routeInfoFactory);

        _node = new Node
        {
            Path = Path,
            Id = Guid.NewGuid(),
            ProxyRef =
                new ProxyRef
                {
                    Id = Guid.NewGuid(),
                    ParseContent = false,
                    PartialMatch = true
                },
            RendererRef =
                new RendererRef
                {
                    Id = "blogRecord",
                    IsPartialMatchRoot = true
                }
        };

        _nodeService = Substitute.For<INodeService>();
        _nodeService.GetByPath(Path).Returns(_node);

        _sut = new RouteService(
            blockClusterConfig,
            _nodeService,
            _publishingService,
            routeInfoFactory,
            requestContext,
            cacheKeyService,
            logger);
    }

    public async Task WhenTheRouteIsRequested()
    {
        _result = await _sut.GetRouteForRequest(new Uri(Host + Path), new Headers());
    }

    public void ThenTheCorrectNodeIsLookedUp()
    {
        _nodeService.Received(1).GetByPath(Path);
    }

    public void AndThenTheCorrectRouteInfoIsReturned()
    {
        Assert.That(_result, Is.Not.Null);
        Assert.That(
            _result.Uri.ToString(),
            Is.EqualTo($"http://website.com/website/Record-single-pagelet.html?nodeId={_node.Id}"));

        var expectedRoutePrefix =
            $"{Constants.Paths.StaticPathUniquePrefix}{RouteInfo.GetUrlFriendlyHash(_projectUuid)}{Constants.Paths.StaticPathUniquePrefix}{_publishingService.GetBlockById("blogs")?.Uuid}";
        Assert.That(_result.RoutePrefix, Is.EqualTo(expectedRoutePrefix));
    }

    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}