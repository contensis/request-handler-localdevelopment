using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.Interfaces;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Resolving.GenericResponseResolverSpecs;

class ResolveStaticPaths
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
        var html = SpecHelper.GetFile("Resolving/Files/site.css");
        var routeInfo = SpecHelper.CreateBasicRouteInfo(_projectUuid, _blockVersionId);

        _result = await _sut.Resolve(html, routeInfo, 0, CancellationToken.None);
    }

    [Then]
    public void ThenTheTargetedStaticPathsAreRewrittenWithABlockVersionIdPrefix()
    {
        var expected =
            $"url(\"/{Constants.Paths.StaticPathUniquePrefix}{RouteInfo.GetUrlFriendlyHash(_projectUuid)}{Constants.Paths.StaticPathUniquePrefix}{_blockVersionId}/static/css/cat.png\")";
        Assert.That(_result, Contains.Substring(expected));
    }

    [AndThen]
    public void AndThenTheRestOfTheStaticPathsAreNotRewritten()
    {
        var expectedUrls = new[]
        {
            "url(\"/another/path/to/images/image01.jpg\")",
            "url(\"/images/image01.jpg\")",
            "url(\"/some/path/to/static.jpg\")"
        };

        foreach (var expectedUrl in expectedUrls)
        {
            Assert.That(_result, Contains.Substring(expectedUrl));
        }
    }

    [Test]
    public void Run()
    {
        this.BDDfy();
    }
}