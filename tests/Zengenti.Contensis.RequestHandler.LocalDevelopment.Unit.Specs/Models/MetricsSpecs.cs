using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Models
{
    namespace MetricSpecs
    {
        public class HasMetrics
        {
            private Metrics _sut = new();
            private string _result;

            [Given]
            public void GivenThereAreSomeMetrics()
            {
                _sut.Add("metric1", 100);
                _sut.Add("metric2", 200);
                _sut.Add("metric3", 300);
            }

            [When]
            public void WhenToStringIsInvoked()
            {
                _result = _sut.ToString();
            }

            [Then]
            public void ThenTheMetricsAreFormattedCorrectly()
            {
                Assert.That(_result, Is.EqualTo("metric1: 100ms, metric2: 200ms, metric3: 300ms"));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        public class HasNoMetrics
        {
            private Metrics _sut = new();
            private string _result;

            [Given]
            public void GivenThereAreSomeMetrics()
            {
            }

            [When]
            public void WhenToStringIsInvoked()
            {
                _result = _sut.ToString();
            }

            [Then]
            public void ThenTheMetricsAreFormattedCorrectly()
            {
                Assert.That(_result, Is.EqualTo(""));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }
    }
}
    
    
