using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Models
{
    namespace MetricSpecs
    {
        public class HasMetrics
        {
            private readonly Metrics _sut = new();
            private string _result;

            public void GivenThereAreSomeMetrics()
            {
                _sut.Add("metric1", 100);
                _sut.Add("metric2", 200);
                _sut.Add("metric3", 300);
            }

            public void WhenToStringIsInvoked()
            {
                _result = _sut.ToString();
            }

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
            private readonly Metrics _sut = new();
            private string _result;

            public void GivenThereAreSomeMetrics()
            {
            }

            public void WhenToStringIsInvoked()
            {
                _result = _sut.ToString();
            }

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