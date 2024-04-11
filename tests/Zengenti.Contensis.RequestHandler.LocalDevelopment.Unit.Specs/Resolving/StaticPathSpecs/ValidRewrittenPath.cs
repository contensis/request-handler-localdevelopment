using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Resolving;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Resolving.StaticPathSpecs;

class ValidRewrittenPath
{
    string _path;
    StaticPath _result;

    public void GivenARewrittenStaticPath()
    {
    }

    public void WhenItIsParsed()
    {
        _result = StaticPath.Parse(_path);
    }

    public void ThenItIsUnderstoodToBeRewritten()
    {
        Assert.That(_result.IsRewritten, Is.True);
    }

    public void AndTheTheOriginalPathIsCorrect()
    {
        Assert.That(_result.OriginalPath, Is.EqualTo("/images/fight-club.png"));
    }

    public void AndTheThePrefixValueIsCorrect()
    {
        Assert.That(_result.BlockVersionId.ToString(), Is.EqualTo("a8cbb345-7f9e-4027-88d0-a684892f9d52"));
    }

    [TestCase("_Gp98VQ_a8cbb345-7f9e-4027-88d0-a684892f9d52/images/fight-club.png")]
    [TestCase("/_Gp98VQ_a8cbb345-7f9e-4027-88d0-a684892f9d52/images/fight-club.png")]
    public void Run(string path)
    {
        _path = path;
        this.BDDfy();
    }
}