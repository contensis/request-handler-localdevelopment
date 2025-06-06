﻿using Microsoft.Extensions.Logging;
using NSubstitute;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.RouteServiceSpecs;

public class PathIsRewrittenStaticResource
{
    private string _requestPath;
    private RouteService _sut;
    private INodeService _nodeService;
    private RouteInfo _result;
    private readonly Guid _projectUuid = Guid.NewGuid();

    public void GivenARequestPathIsARewrittenStaticResourcePath()
    {
        _nodeService = Substitute.For<INodeService>();

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.ProjectUuid.Returns(_projectUuid);
        var cacheKeyService = Substitute.For<ICacheKeyService>();
        var logger = Substitute.For<ILogger<RouteService>>();
        var blockClusterConfig = new AppConfiguration();
        var routeInfoFactory =
            new RouteInfoFactory(requestContext, blockClusterConfig);
        var publishingService = SpecHelper.CreatePublishingService(routeInfoFactory);
        var block = publishingService.GetBlockById("blogs");

        _requestPath =
            $"/{Constants.Paths.StaticPathUniquePrefix}{RouteInfo.GetUrlFriendlyHash(_projectUuid)}{Constants.Paths.StaticPathUniquePrefix}{block.Uuid}/static/images/header.png";

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
        _result = await _sut.GetRouteForRequest(new Uri("http://www.origin.com" + _requestPath), new Headers());
    }

    public void ThenNoNodeIsLookedUp()
    {
        _nodeService.DidNotReceive().GetByPath(_requestPath);
    }

    public void AndThenARouteIsReturnedWithTheInternalResourceUri()
    {
        Assert.That(_result, Is.Not.Null);
        Assert.That(
            _result.Uri.ToString(),
            Is.EqualTo("http://website.com/static/images/header.png"));
    }

    public void AndThenTheCorrectBlockVersionStaticPathsAreSet()
    {
        Assert.That(_result.BlockVersionInfo, Is.Not.Null);
        Assert.That(_result.BlockVersionInfo!.StaticPaths, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(_result.BlockVersionInfo.StaticPaths, Does.Contain("/static"));
    }

    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}