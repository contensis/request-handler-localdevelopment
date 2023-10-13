using Zengenti.Contensis.RequestHandler.Application.Parsing;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Parsing
{
    public static class ParserAssert
    {
        public static void TagAccurate(HtmlTag tag, string name, int length)
        {
            Assert.That(tag.Name == name, $"Tag name '{tag.Name}' should be {name}.");
            Assert.That(tag.Length == length, $"Tag length '{tag.Length}' should be {length}.");
        }
    }
}