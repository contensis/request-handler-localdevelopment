using Microsoft.Extensions.Logging;
using NSubstitute;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteServiceSpecs;

public class ShouldNotPerformNodeLookupIfExcludedPath
{
    private RouteService _sut;
    private INodeService _nodeService;
    private string _relativePathAndQuery;

    public void GivenARequestPathIsAnExcludedPath()
    {
        _nodeService = Substitute.For<INodeService>();
        var publishingService = Substitute.For<IPublishingService>();
        var requestContext = Substitute.For<IRequestContext>();
        var cacheKeyService = Substitute.For<ICacheKeyService>();
        requestContext.Alias.Returns("test");
        var logger = Substitute.For<ILogger<RouteService>>();
        var routeInfoFactory = Substitute.For<IRouteInfoFactory>();
        routeInfoFactory.CreateForNonNodePath(Arg.Any<Uri>(), Arg.Any<Headers>(), Arg.Any<BlockVersionInfo?>())
            .Returns(new RouteInfo(new Uri("http://www.mysite.com"), new Headers(), "nodePath", true));

        _sut = new RouteService(
            new AppConfiguration(),
            _nodeService,
            publishingService,
            routeInfoFactory,
            requestContext,
            cacheKeyService,
            logger);
    }

    public async Task WhenTheRouteIsRequested()
    {
        await _sut.GetRouteForRequest(new Uri("http://www.mysite.com" + _relativePathAndQuery), new Headers());
    }

    public void ThenANodeLookupIsNotInvoked()
    {
        var indexOfStartOfQuery = _relativePathAndQuery.IndexOf("?", StringComparison.InvariantCultureIgnoreCase);
        var relativePath = indexOfStartOfQuery > -1
            ? _relativePathAndQuery.Substring(
                0,
                indexOfStartOfQuery)
            : _relativePathAndQuery;

        _nodeService.DidNotReceive().GetByPath(relativePath);
    }

    [TestCase("/favicon.ico"),
     TestCase("/api/delivery/projects/website"),
     TestCase("/API/some-method"),
     TestCase("/contensis-preview-toolbar/"),
     TestCase("/contensis-preview-toolbar/?some-stuff"),
     TestCase("/rest/ui/formsmodule/testaccessibility/"),
     TestCase("/REST/UI/FormsModule/TestAccessibility/"),
     TestCase("/REST/UI/FormsModule/TestAccessibility/?some-stuff"),
     TestCase("/REST/Contensis/content/GetFormSettings")]
    public void Run(string relativePathAndQuery)
    {
        _relativePathAndQuery = relativePathAndQuery;
        this.BDDfy();
    }
}