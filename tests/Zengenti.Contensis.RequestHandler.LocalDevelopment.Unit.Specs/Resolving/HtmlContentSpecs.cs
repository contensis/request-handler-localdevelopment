using Microsoft.Extensions.Logging;
using NSubstitute;
using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Parsing;
using Zengenti.Contensis.RequestHandler.Application.Resolving;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Resolving
{
    namespace HtmlContentSpecs
    {
        class MultiplePageletsResolvedInOrder
        {
            private HtmlContent _sut;
            private readonly List<Guid> _tagIds = new List<Guid>();

            [Given]
            public void GivenMultiplePagelets()
            {
                var html = SpecHelper.GetFile("Resolving/Files/Record-multiple-pagelets.html");
                var parser = new HtmlParser(html);

                _sut = new HtmlContent(html, Substitute.For<ILogger>());

                while (parser.ParseNext(
                    new[]
                    {
                        "pagelet"
                    },
                    out var tag))
                {
                    _tagIds.Add(tag.Id);
                    _sut.AddTagOffset(tag);
                }
            }

            [When]
            public async Task WhenThePageletsAreResolvedInOrder()
            {
                await _sut.UpdateTag(_tagIds[0], SpecHelper.GetFile("Resolving/Files/Pagelet1.html"));
                await _sut.UpdateTag(_tagIds[1], SpecHelper.GetFile("Resolving/Files/Pagelet2.html"));
                await _sut.UpdateTag(_tagIds[2], SpecHelper.GetFile("Resolving/Files/Pagelet3.html"));
            }

            [Then]
            public void ThenThePageletsAreResolvedCorrectly()
            {
                var result = _sut.ToString().NormalizeLineEndings();
                ResolvingAssert.StartPositionCorrect(result, 367);
                ResolvingAssert.EndPositionCorrect(result, 648);
                ResolvingAssert.StartPositionCorrect(result, 804);
                ResolvingAssert.EndPositionCorrect(result, 1135);
                ResolvingAssert.StartPositionCorrect(result, 1676);
                ResolvingAssert.EndPositionCorrect(result, 2101);
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        class MultiplePageletsResolvedUnordered
        {
            private HtmlContent _sut;
            private readonly List<Guid> _tagIds = new List<Guid>();

            [Given]
            public void GivenMultiplePagelets()
            {
                var html = SpecHelper.GetFile("Resolving/Files/Record-multiple-pagelets.html");
                var parser = new HtmlParser(html);

                _sut = new HtmlContent(html, Substitute.For<ILogger>());

                while (parser.ParseNext(
                    new[]
                    {
                        "pagelet"
                    },
                    out var tag))
                {
                    _tagIds.Add(tag.Id);
                    _sut.AddTagOffset(tag);
                }
            }

            [When]
            public async Task WhenThePageletsAreResolvedUnOrdered()
            {
                await _sut.UpdateTag(_tagIds[2], SpecHelper.GetFile("Resolving/Files/Pagelet3.html"));
                await _sut.UpdateTag(_tagIds[1], SpecHelper.GetFile("Resolving/Files/Pagelet2.html"));
                await _sut.UpdateTag(_tagIds[0], SpecHelper.GetFile("Resolving/Files/Pagelet1.html"));
            }

            [Then]
            public void ThenThePageletsAreResolvedCorrectly()
            {
                var result = _sut.ToString().NormalizeLineEndings();
                ResolvingAssert.StartPositionCorrect(result, 367);
                ResolvingAssert.EndPositionCorrect(result, 648);
                ResolvingAssert.StartPositionCorrect(result, 804);
                ResolvingAssert.EndPositionCorrect(result, 1135);
                ResolvingAssert.StartPositionCorrect(result, 1676);
                ResolvingAssert.EndPositionCorrect(result, 2101);
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        class HtmlTagIdDoesNotExist
        {
            private HtmlContent _sut;

            [Given]
            public void GivenAHtmlTagIdDoesNotExist()
            {
                var html = SpecHelper.GetFile("Resolving/Files/Record-single-pagelet.html");
                var parser = new HtmlParser(html);

                _sut = new HtmlContent(html, Substitute.For<ILogger>());

                while (parser.ParseNext(
                    new[]
                    {
                        "pagelet"
                    },
                    out var tag))
                {
                    _sut.AddTagOffset(tag);
                }
            }

            [When]
            public async Task WhenTheTagUpdateIsApplied()
            {
                await _sut.UpdateTag(new Guid(), SpecHelper.GetFile("Resolving/Files/Pagelet1.html"));
            }

            [Then]
            public void ThenTheUpdateIsIgnored()
            {
                Assert.That(_sut.ToString().NormalizeLineEndings().Length, Is.EqualTo(782));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        class HtmlTagContentUpdateIsNull
        {
            private HtmlContent _sut;
            private Guid _tagId;

            [Given]
            public void GivenAPageletResponseIsNull()
            {
                var html = SpecHelper.GetFile("Resolving/Files/Record-single-pagelet.html");
                var parser = new HtmlParser(html);

                _sut = new HtmlContent(html, Substitute.For<ILogger>());

                while (parser.ParseNext(
                    new[]
                    {
                        "pagelet"
                    },
                    out var tag))
                {
                    _tagId = tag.Id;
                    _sut.AddTagOffset(tag);
                }
            }

            [When]
            public async Task WhenTheTagUpdateIsApplied()
            {
                await _sut.UpdateTag(_tagId, null);
            }

            [Then]
            public void ThenTheUpdateIsIgnored()
            {
                Assert.That(_sut.ToString().NormalizeLineEndings().Length, Is.EqualTo(751));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }
    }
}