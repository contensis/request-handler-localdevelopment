using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Resolving;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Resolving.StaticPathSpecs;

class ValidNonRewrittenPath
{
    string _path;
    StaticPath _result;

    [Given]
    public void GivenANonRewrittenStaticPath()
    {
    }

    [When]
    public void WhenItIsParsed()
    {
        _result = StaticPath.Parse(_path);
    }

    [Then]
    public void ThenItIsUnderstoodToNotBeRewritten()
    {
        Assert.That(_result.IsRewritten, Is.False);
    }

    [AndThen]
    public void AndThenThePathPropertiesAreTheSame()
    {
        Assert.That(_result.Path, Is.EqualTo(_path));
        Assert.That(_result.OriginalPath, Is.EqualTo(_path));
    }

    [TestCase("/")]
    [TestCase("/some/other/folder/with/images/in/path/image01.jpg")]
    [TestCase("/some/other/folder/with/static/in/path/image01.jpg")]
    [TestCase("/images/static.png")]
    [TestCase("/images/fight-club.png")]
    public void Run(string path)
    {
        _path = path;
        this.BDDfy();
    }
}