using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Resolving.GenericResponseResolverSpecs;

class ResolveEncodedStaticPaths
{
    IResponseResolver _sut;
    private string _result;
    private readonly Guid _blockVersionId = Guid.NewGuid();
    private readonly Guid _projectUuid = Guid.NewGuid();

    [Given]
    public void GivenTheGenericResponseResolver()
    {
        _sut = new GenericResponseResolver();
    }

    [When]
    public async void WhenAResponseContainsStaticLinks()
    {
        var html = SpecHelper.GetFile("Resolving/Files/encoded.json");
        var routeInfo = SpecHelper.CreateBasicRouteInfo(_projectUuid, _blockVersionId, "/static");

        _result = await _sut.Resolve(html, routeInfo, 0, CancellationToken.None);
    }

    [Then]
    public void ThenTheStaticPathsAreRewrittenWithABlockVersionIdPrefix()
    {
        var expected =
            $"\\u002F{Constants.Paths.StaticPathUniquePrefix}{RouteInfo.GetUrlFriendlyHash(_projectUuid)}{Constants.Paths.StaticPathUniquePrefix}{_blockVersionId}\\u002Fstatic\\u002Fimage-library\\u002Fcat.x38c2c8f4.png";
        Assert.That(_result, Contains.Substring("\"" + $"{expected}" + "\""));
    }

    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}