﻿using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Resolving;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Resolving.StaticPathSpecs;

class InvalidRewrittenPath
{
    string _path;
    StaticPath _result;

    [Given]
    public void GivenARewrittenStaticPath()
    {
    }

    [When]
    public void WhenItIsParsed()
    {
        _result = StaticPath.Parse(_path);
    }

    [Then]
    public void ThenItIsNotRewritten()
    {
        Assert.That(_result.IsRewritten, Is.False);
    }

    [AndThen]
    public void AndTheTheOriginalPathIsCorrect()
    {
        Assert.That(_result.OriginalPath, Is.EqualTo(_path));
    }

    [TestCase("/_Gp98VQ_a/images/fight-club.png")]
    [TestCase("/_Gp98VQ_/images/fight-club.png")]
    [TestCase("/_blah/images/fight-club.png")]
    public void Run(string path)
    {
        _path = path;
        this.BDDfy();
    }
}