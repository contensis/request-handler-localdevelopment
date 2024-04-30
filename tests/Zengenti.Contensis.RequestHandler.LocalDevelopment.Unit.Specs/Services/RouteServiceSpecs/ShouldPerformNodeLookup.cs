using Microsoft.Extensions.Logging;
using NSubstitute;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteServiceSpecs;

public class ShouldPerformNodeLookup
{
    private RouteService _sut;
    private INodeService _nodeService;
    private string _requestPathAndQuery;
    private string _nodePathAndQuery;

    public void GivenARequestPathIsAnExcludedPath()
    {
        _nodeService = Substitute.For<INodeService>();
        var publishingService = Substitute.For<IPublishingService>();
        var requestContext = Substitute.For<IRequestContext>();
        var cacheKeyService = Substitute.For<ICacheKeyService>();
        requestContext.Alias.Returns("test");
        var logger = Substitute.For<ILogger<RouteService>>();
        var routeInfoFactory = Substitute.For<IRouteInfoFactory>();

        _sut = new RouteService(
            new BlockClusterConfig(),
            _nodeService,
            publishingService,
            routeInfoFactory,
            requestContext,
            cacheKeyService,
            logger);
    }

    public async Task WhenTheRouteIsRequested()
    {
        await _sut.GetRouteForRequest(new Uri("http://www.mysite.com" + _requestPathAndQuery), new Headers());
    }

    public void ThenANodeLookupIsInvoked()
    {
        var indexOfStartOfQuery = _nodePathAndQuery.IndexOf("?", StringComparison.InvariantCultureIgnoreCase);
        var relativePath = indexOfStartOfQuery > -1
            ? _nodePathAndQuery.Substring(
                0,
                indexOfStartOfQuery)
            : _nodePathAndQuery;

        _nodeService.Received().GetByPath(relativePath);
    }

    [TestCase("/", "/"),
     TestCase("/blah", "/blah"),
     TestCase("/blah/", "/blah"),
     TestCase("/blah/blah/blah?some-stuff", "/blah/blah/blah?some-stuff"),
     TestCase("/blah/blah/blah/?some-stuff", "/blah/blah/blah?some-stuff")]
    public void Run(string requestPathAndQuery, string nodePathAndQuery)
    {
        _requestPathAndQuery = requestPathAndQuery;
        _nodePathAndQuery = nodePathAndQuery;
        this.BDDfy();
    }
}