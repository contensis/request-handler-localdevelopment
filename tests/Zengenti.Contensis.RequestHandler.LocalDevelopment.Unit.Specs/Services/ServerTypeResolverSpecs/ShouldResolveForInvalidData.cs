using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.ServerTypeResolverSpecs;

public class ShouldResolveForInvalidData
{
    private ServerTypeResolver _sut;
    private ServerType _serverType;

    [Given]
    public void GivenAServerTypeResolver()
    {
        _sut = new ServerTypeResolver();
    }

    [When]
    public void WhenTheServerTypeIsResolvedForInvalidData()
    {
        _serverType = _sut.GetServerType();
    }

    [Then]
    public void ThenTheServerTypeIsCorrect()
    {
        Assert.That(_serverType, Is.EqualTo(ServerType.Live));
    }

    [TestCase(null),
     TestCase(""),
     TestCase("  "),
     TestCase("something")]
    public void Run(string serverTypeAsString)
    {
        CallContext.Current[Constants.Headers.ServerType] = serverTypeAsString;
        this.BDDfy();
    }
}