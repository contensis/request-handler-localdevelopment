using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Domain.Common;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Models.Headers
{
    namespace CreateHeaderSpecs
    {
        public class CreateFromAnIEnumerableDictionary
        {
            private Domain.ValueTypes.Headers _result;

            public void WhenHeadersAreCreatedFromAnIEnumerableDictionary()
            {
                _result = new Domain.ValueTypes.Headers(
                    new Dictionary<string, IEnumerable<string>>
                    {
                        {
                            Constants.Headers.Alias, new[]
                            {
                                "zenhub"
                            }
                        },
                        {
                            Constants.Headers.ProjectApiId, new[]
                            {
                                "contensis"
                            }
                        }
                    });
            }

            public void ThenTheValuesAreCorrectlySet()
            {
                Assert.That(_result.Values.Count, Is.EqualTo(2));
                Assert.That(_result.Values[Constants.Headers.Alias].FirstOrDefault(), Is.EqualTo("zenhub"));
                Assert.That(_result.Values[Constants.Headers.ProjectApiId].FirstOrDefault(), Is.EqualTo("contensis"));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        public class CreateFromASingleValueDictionary
        {
            private Domain.ValueTypes.Headers _result;

            public void WhenHeadersAreCreatedFromASingleValueDictionary()
            {
                _result = new Domain.ValueTypes.Headers(
                    new Dictionary<string, string>
                    {
                        {
                            Constants.Headers.Alias, "zenhub"
                        },
                        {
                            Constants.Headers.ProjectApiId, "contensis"
                        }
                    });
            }

            public void ThenTheValuesAreCorrectlySet()
            {
                Assert.That(_result.Values.Count, Is.EqualTo(2));
                Assert.That(_result.Values[Constants.Headers.Alias].FirstOrDefault(), Is.EqualTo("zenhub"));
                Assert.That(_result.Values[Constants.Headers.ProjectApiId].FirstOrDefault(), Is.EqualTo("contensis"));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        public class CreateFromAnIHeaderDictionary
        {
            private Domain.ValueTypes.Headers _result;

            public void WhenHeadersAreCreatedFromAnIHeaderDictionary()
            {
                _result = new Domain.ValueTypes.Headers(
                    new HeaderDictionary(
                        new Dictionary<string, StringValues>
                        {
                            {
                                Constants.Headers.Alias, "zenhub"
                            },
                            {
                                Constants.Headers.ProjectApiId, "contensis"
                            }
                        }));
            }

            public void ThenTheValuesAreCorrectlySet()
            {
                Assert.That(_result.Values.Count, Is.EqualTo(2));
                Assert.That(_result.Values[Constants.Headers.Alias].FirstOrDefault(), Is.EqualTo("zenhub"));
                Assert.That(_result.Values[Constants.Headers.ProjectApiId].FirstOrDefault(), Is.EqualTo("contensis"));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        public class CreateFromHttpResponseHeaders
        {
            private Domain.ValueTypes.Headers _result;

            public void WhenHeadersAreCreatedFromHttpResponseHeaders()
            {
                var response = new HttpResponseMessage();
                response.Headers.Add(Constants.Headers.Alias, "zenhub");
                response.Headers.Add(Constants.Headers.ProjectApiId, "contensis");

                _result = new Domain.ValueTypes.Headers(response.Headers);
            }

            public void ThenTheValuesAreCorrectlySet()
            {
                Assert.That(_result.Values.Count, Is.EqualTo(2));
                Assert.That(_result.Values[Constants.Headers.Alias].FirstOrDefault(), Is.EqualTo("zenhub"));
                Assert.That(_result.Values[Constants.Headers.ProjectApiId].FirstOrDefault(), Is.EqualTo("contensis"));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }
    }
}