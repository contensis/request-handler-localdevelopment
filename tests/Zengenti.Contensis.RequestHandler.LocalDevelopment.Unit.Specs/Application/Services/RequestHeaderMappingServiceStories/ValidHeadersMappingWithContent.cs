using FluentAssertions;
using FluentAssertions.Execution;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Services;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Application.Services.
    RequestHeaderMappingServiceStories;

internal class ValidHeadersMappingWithContent
{
    private HttpRequestMessage _requestMessage;
    private Dictionary<string, IEnumerable<string>> _headersToMap;

    internal void GivenARequestMessageWithContent()
    {
        _requestMessage = new HttpRequestMessage();
        _requestMessage.Content = new StringContent("Some content");
    }

    internal void AndGivenASetOfHeadersToMap()
    {
        _headersToMap = new HashSet<string>(RequestHeaderMappingServiceStory.StandardRequestHeaders)
            .Union(RequestHeaderMappingServiceStory.StandardEntityHeaders)
            .Union(RequestHeaderMappingService.DisallowedRequestHeaderMappings)
            .ToDictionary(
                key => key,
                _ => new[]
                {
                    "some header value"
                }.AsEnumerable());
    }

    internal void WhenTheHeadersAreMappedToTheRequest()
    {
        new RequestHeaderMappingService().MapHeaders(
            _requestMessage,
            _headersToMap);
    }

    internal void ThenDisallowedRequestHeadersAreNotMappedToTheRequest()
    {
        _requestMessage.Headers
            .Select(x => x.Key)
            .Should()
            .NotContain(RequestHeaderMappingService.DisallowedRequestHeaderMappings);
    }

    internal void AndThenTheAllowedStandardHeadersAreMappedToTheRequest()
    {
        var allowedHeaderKeys = RequestHeaderMappingServiceStory.StandardRequestHeaders
            .Where(
                x => !RequestHeaderMappingService.DisallowedRequestHeaderMappings.Any(y => y.EqualsCaseInsensitive(x)))
            .ToArray();

        using (new AssertionScope())
        {
            _requestMessage.Headers.Count().Should().Be(allowedHeaderKeys.Length);
            foreach (var allowedHeaderKey in allowedHeaderKeys)
            {
                _requestMessage.Headers
                    .Any(x => x.Key.EqualsCaseInsensitive(allowedHeaderKey))
                    .Should()
                    .BeTrue($"Header {allowedHeaderKey} was not mapped to headers");
            }
        }
    }

    internal void AndThenTheAllowedEntityHeadersAreMappedToTheRequest()
    {
        _requestMessage.Content.Should().NotBeNull();

        var allowedHeaderKeys = RequestHeaderMappingServiceStory.StandardEntityHeaders
            .Where(
                x => !RequestHeaderMappingService.DisallowedRequestHeaderMappings.Any(y => y.EqualsCaseInsensitive(x)))
            .ToArray();

        using (new AssertionScope())
        {
            _requestMessage.Content!.Headers.Count().Should().Be(allowedHeaderKeys.Length);
            foreach (var allowedHeaderKey in allowedHeaderKeys)
            {
                _requestMessage.Content.Headers
                    .Any(x => x.Key.EqualsCaseInsensitive(allowedHeaderKey))
                    .Should()
                    .BeTrue($"Header {allowedHeaderKey} was not mapped to headers");
            }
        }
    }

    [Test]
    public void RunTest()
    {
        this.BDDfy<RequestHeaderMappingServiceStory>();
    }
}