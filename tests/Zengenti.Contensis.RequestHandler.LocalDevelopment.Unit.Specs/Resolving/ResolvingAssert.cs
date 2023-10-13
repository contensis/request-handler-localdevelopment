namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Resolving
{
    public static class ResolvingAssert
    {
        private static readonly string PageletStartToken = "<!--start-->";
        private static readonly string PageletEndToken = "<!--end-->";
        
        public static void StartPositionCorrect(string html, int pos)
        {
            Assert.That(html.Substring(pos, PageletStartToken.Length), Is.EqualTo(PageletStartToken));
        }
        
        public static void EndPositionCorrect(string html, int pos)
        {
            Assert.That(html.Substring(pos, PageletEndToken.Length), Is.EqualTo(PageletEndToken));
        } 
    }
}