using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Domain.Common;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Models.Headers
{
    namespace HeaderMergeSpecs
    {
        public class MatchingHeadersExists
        {
            private readonly Domain.ValueTypes.Headers _sut = new();
            private Domain.ValueTypes.Headers _result;

            [Given]
            public void GivenHeadersExist()
            {
                _sut.Add(
                    Constants.Headers.Alias,
                    new[]
                    {
                        "zenhub"
                    });
                _sut.Add(
                    Constants.Headers.ProjectApiId,
                    new[]
                    {
                        "contensis"
                    });
            }

            [When]
            public void WhenAMatchingHeaderValueIsMerged()
            {
                var toMerge = new Domain.ValueTypes.Headers();
                toMerge.Add(
                    Constants.Headers.Alias,
                    new[]
                    {
                        "develop"
                    });

                _result = _sut.Merge(toMerge);
            }

            [Then]
            public void ThenTheMatchingHeaderValueIsOverridden()
            {
                Assert.That(_result.Values.Count, Is.EqualTo(2));
                Assert.That(_result.Values[Constants.Headers.Alias].FirstOrDefault(), Is.EqualTo("develop"));
                Assert.That(_result.Values[Constants.Headers.ProjectApiId].FirstOrDefault(), Is.EqualTo("contensis"));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        public class NoMatchingHeadersExist
        {
            private readonly Domain.ValueTypes.Headers _sut = new();
            private Domain.ValueTypes.Headers _result;

            [Given]
            public void GivenHeadersExist()
            {
                _sut.Add(
                    Constants.Headers.Alias,
                    new[]
                    {
                        "zenhub"
                    });
                _sut.Add(
                    Constants.Headers.ProjectApiId,
                    new[]
                    {
                        "contensis"
                    });
            }

            [When]
            public void WhenAHeaderValueIsMergedThatDoesNotAlreadyExist()
            {
                var toMerge = new Domain.ValueTypes.Headers();
                toMerge.Add(
                    Constants.Headers.ProjectUuid,
                    new[]
                    {
                        Guid.Empty.ToString()
                    });

                _result = _sut.Merge(toMerge);
            }

            [Then]
            public void ThenTheNewHeaderValueIsAdded()
            {
                Assert.That(_result.Values.Count, Is.EqualTo(3));
                Assert.That(_result.Values[Constants.Headers.Alias].FirstOrDefault(), Is.EqualTo("zenhub"));
                Assert.That(_result.Values[Constants.Headers.ProjectApiId].FirstOrDefault(), Is.EqualTo("contensis"));
                Assert.That(
                    _result.Values[Constants.Headers.ProjectUuid].FirstOrDefault(),
                    Is.EqualTo(Guid.Empty.ToString()));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        public class CaseInsensitiveKey
        {
            private readonly Domain.ValueTypes.Headers _sut = new();
            private Domain.ValueTypes.Headers _result;

            [Given]
            public void GivenAHeadersExistsThatIsHandledCaseInsensitively()
            {
                _sut.Add(
                    "host",
                    new[]
                    {
                        "http://www.mywebsite.com"
                    });
            }

            [When]
            public void WhenAHeaderWithTheSameNameButDifferentCaseIsMerged()
            {
                var toMerge = new Domain.ValueTypes.Headers();
                toMerge.Add(
                    "host",
                    new[]
                    {
                        "http://www.myotherwebsite.com"
                    });

                _result = _sut.Merge(toMerge);
            }

            [Then]
            public void ThenTheHeaderValueIsOverriden()
            {
                Assert.That(_result.Values.Count, Is.EqualTo(1));
                Assert.That(_result.Values["host"].FirstOrDefault(), Is.EqualTo("http://www.myotherwebsite.com"));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }
    }
}