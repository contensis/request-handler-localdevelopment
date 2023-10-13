using Zengenti.Contensis.RequestHandler.Application.Resolving;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs
{
    [SetUpFixture]
    public class NUnitConfig
    {
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            HtmlResponseResolver.ParsePagelets = true;
        }
    }
}
