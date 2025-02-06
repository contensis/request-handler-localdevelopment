using System.Net.Http.Headers;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Resolving;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Resolving
{
    namespace HtmlResponseResolverSpecs
    {
        internal class ResolvePagelets
        {
            private HtmlResponseResolver _sut;
            private string _result;

            public void GivenTheHtmlResponseResolver()
            {
                _sut = SpecHelper.CreateHtmlResponseResolver();

                SpecHelper.SetEndpointResponse(
                    _sut.RequestService,
                    "/website/pagelet1.html",
                    SpecHelper.GetFile("Resolving/Files/Pagelet1.html"));
            }

            public async Task WhenHtmlWithSinglePageletIsResolved()
            {
                var html = SpecHelper.GetFile("Resolving/Files/Record-single-pagelet.html");

                var blockVersionInfo =
                    new BlockVersionInfo(
                        Guid.NewGuid(),
                        "",
                        Guid.NewGuid(),
                        new Uri("http://website.com"),
                        "master",
                        false,
                        null,
                        1);

                var routeInfo = new RouteInfo(new Uri("http://website.com"), new Headers(), "", true, blockVersionInfo);

                _result = await _sut.Resolve(html, routeInfo, 0, CancellationToken.None);
            }

            public void ThenTheResultIsCorrect()
            {
                Assert.That(_result, Is.Not.Null.Or.Empty);
                Assert.That(_result, Does.Not.Contain("<pagelet renderer"));
                Assert.That(_result, Does.Not.Contain("<!--pagelet-not-resolved-->"));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        internal class ResolvePageletsWithTraceEnabled
        {
            private HtmlResponseResolver _sut;
            private string _result;

            public void GivenTheHtmlResponseResolverWithTraceEnabled()
            {
                _sut = SpecHelper.CreateHtmlResponseResolver(true);

                HttpRequestMessage requestMessage =
                    new HttpRequestMessage(HttpMethod.Get, "http://website.com/website/pagelet1.html")
                    {
                        Headers =
                        {
                            Host = "website.com",
                            Accept =
                            {
                                new MediaTypeWithQualityHeaderValue("application/json"),
                                new MediaTypeWithQualityHeaderValue("text/xml")
                            }
                        }
                    };
                SpecHelper.SetEndpointResponse(
                    _sut.RequestService,
                    "/website/pagelet1.html",
                    SpecHelper.GetFile("Resolving/Files/Pagelet1.html"),
                    pageletPerformanceData: new PageletPerformanceData(
                        5,
                        5,
                        10,
                        requestMessage.RequestUri,
                        requestMessage.Method,
                        requestMessage.Headers));
            }

            public async Task WhenHtmlWithSinglePageletIsResolved()
            {
                var html = SpecHelper.GetFile("Resolving/Files/Record-single-pagelet.html");

                var routeInfo = new RouteInfo(
                    new Uri("http://website.com"),
                    new Headers(),
                    "",
                    true,
                    new BlockVersionInfo(
                        Guid.NewGuid(),
                        "",
                        Guid.NewGuid(),
                        new Uri("http://website.com"),
                        "master",
                        false,
                        null,
                        1));

                _result = await _sut.Resolve(html, routeInfo, 0, CancellationToken.None);
            }

            public void ThenTheResultIsCorrect()
            {
                var separator = Environment.NewLine;
                Assert.That(_result, Is.Not.Null.Or.Empty);
                Assert.That(
                    _result,
                    Does.Contain(
                        $"<!-- {separator} durationMs = 10 {separator} requestMs = 5 {separator} parsingMs = 5 "));
                Assert.That(
                    _result,
                    Does.Contain(
                        $@"curl -H ""Host: website.com"" -H ""Accept: application/json; text/xml"" --request GET http://website.com/website/pagelet1.html {separator} -->"));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        //class ResolvePageletsWithSingleEndpointFailure
        //{
        //    private HtmlResponseResolver _sut;
        //    private string _result;
        //    private Exception _ex;

        //    
        //    public void GivenTheHtmlResponseResolver()
        //    {

        //        _sut = SpecHelper.CreateHtmlResponseResolver();

        //        SpecHelper.SetEndpointResponse(_sut.RequestService, "/website/pagelet1.html",
        //            "500 error", 500);

        //    }

        //    
        //    public async Task WhenHtmlWithSinglePageletIsResolved()
        //    {
        //        var html = SpecHelper.GetFile("Resolving/Files/Record-single-pagelet.html");
        //        var block = new Block { BaseUri = new Uri("http://website.com") };

        //        var routeInfo = new RouteInfo(
        //            new Uri("http://website.com"),
        //            block,
        //            new Endpoint { Block = block },
        //            new Node(),
        //            new Renderer()
        //        );

        //        try
        //        {
        //            _result = await _sut.Resolve(html, routeInfo, 0, CancellationToken.None);
        //        }
        //        catch (Exception ex)
        //        {
        //            _ex = ex;
        //        }
        //    }

        //    
        //    public void ThenAnEndpointExceptionIsThrown()
        //    {
        //        Assert.That(_result, Is.Null);
        //        Assert.That(_ex, Is.Not.Null.And.AssignableTo<AggregateException>());
        //        Assert.That((_ex as AggregateException).InnerExceptions.Count, Is.EqualTo(1));
        //        Assert.That((_ex as AggregateException).InnerExceptions[0], Is.AssignableTo<EndpointException>());

        //        var endpointException = (_ex as AggregateException).InnerExceptions[0] as EndpointException;
        //        Assert.That(endpointException.EndpointResponse.StatusCode, Is.EqualTo(500));
        //        Assert.That(endpointException.Endpoint, Is.Not.Null);
        //        Assert.That(endpointException.Endpoint.Id, Is.EqualTo("pagelet1"));
        //    }

        //    [Test]
        //    public void Run()
        //    {
        //        this.BDDfy();
        //    }
        //}

        //class ResolvePageletsWithMultipleEndpointsFailure
        //{
        //    private HtmlResponseResolver _sut;
        //    private string _result;
        //    private Exception _ex;

        //    
        //    public void GivenTheHtmlResponseResolver()
        //    {

        //        _sut = SpecHelper.CreateHtmlResponseResolver();

        //        SpecHelper.SetEndpointResponse(_sut.RequestService, "/website/pagelet1.html",
        //            "401 error", 401);
        //        SpecHelper.SetEndpointResponse(_sut.RequestService, "/website/pagelet2.html",
        //            "403 error", 403);
        //        SpecHelper.SetEndpointResponse(_sut.RequestService, "/website/pagelet3.html",
        //           "404 error", 404);
        //    }

        //    
        //    public async Task WhenHtmlWithMultiplePageletsIsResolved()
        //    {
        //        var html = SpecHelper.GetFile("Resolving/Files/Record-multiple-pagelets.html");
        //        var block = new Block { BaseUri = new Uri("http://website.com") };

        //        var routeInfo = new RouteInfo(
        //            new Uri("http://website.com"),
        //            block,
        //            new Endpoint { Block = block },
        //            new Node(),
        //            new Renderer()
        //        );

        //        try
        //        {

        //            _result = await _sut.Resolve(html, routeInfo, 0, CancellationToken.None);
        //        }
        //        catch (Exception ex)
        //        {
        //            _ex = ex;
        //        }
        //    }

        //    
        //    public void ThenAnAggregateExceptionIsThrown()
        //    {
        //        Assert.That(_result, Is.Null);
        //        Assert.That(_ex, Is.Not.Null);
        //        Assert.That(_ex, Is.AssignableTo<AggregateException>());
        //        Assert.That((_ex as AggregateException).InnerExceptions.Count, Is.EqualTo(3));
        //    }

        //    [Test]
        //    public void Run()
        //    {
        //        this.BDDfy();
        //    }
        //}
    }
}