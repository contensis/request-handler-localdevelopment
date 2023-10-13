using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;
using Zengenti.Contensis.RequestHandler.Domain.Common;
using Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Renderers;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Services.ServerTypeResolverSpecs;

public class ShouldResolveForValidData
{
    private ServerTypeResolver _sut;
    private ServerType _serverType;
    private ServerType _expectedServerType;

    [Given]
    public void GivenAServerTypeResolver()
    {
        _sut = new ServerTypeResolver();
    }

    [When]
    public void WhenTheServerTypeIsResolvedForValidData()
    {
        _serverType = _sut.GetServerType();
    }

    [Then]
    public void ThenTheServerTypeIsCorrect()
    {
        Assert.That(_serverType, Is.EqualTo(_expectedServerType));
    }

    [TestCase("liVe", ServerType.Live),
     TestCase("Live", ServerType.Live),
     TestCase("staGing", ServerType.Staging),
     TestCase("Staging", ServerType.Staging),
     TestCase("preView", ServerType.Preview),
     TestCase("Preview", ServerType.Preview),
     TestCase("tesT", ServerType.Preview)]
    public void Run(string serverTypeAsString, ServerType expectedServerType)
    {
        _expectedServerType = expectedServerType;
        CallContext.Current[Constants.Headers.ServerType] = serverTypeAsString;
        this.BDDfy();
    }
}