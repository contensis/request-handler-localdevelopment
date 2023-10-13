using System.Diagnostics.CodeAnalysis;
using TestStack.BDDfy;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Models.RouteInfo;

public class ValidGetUrlFriendlyHash
{
    private string _expectedHash;
    private Guid _id;
    private string _hash;

    public void GivenAValidGuidValue()
    {
    }

    public void WhenGetUrlFriendlyHashIsInvoked()
    {
        _hash = Domain.ValueTypes.RouteInfo.GetUrlFriendlyHash(_id);
    }

    public void ThenTheHashIsCorrect()
    {
        Assert.That(_hash, Is.EqualTo(_expectedHash));
    }

    [TestCase("91561621-48fb-42fd-b32d-b5ee5d5aee28", "w4i9Og"),
     TestCase("cb2b9513-8459-4f01-b726-25a57b17e627", "CFZSWA"),
     TestCase("1071396c-fc91-f48f-0080-f0ccf974ab53", "HWx++w")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public void Run(string id, string hash)
    {
        _id = Guid.Parse(id);
        _expectedHash = hash;
        this.BDDfy();
    }
}