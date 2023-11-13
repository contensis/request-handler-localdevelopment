using TestStack.BDDfy;
using Zengenti.Contensis.RequestHandler.Application.Parsing;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Unit.Specs.Parsing
{
    namespace HtmlParserSpecs
    {
        class SelfClosingTags
        {
            private HtmlParser _sut;
            private readonly List<HtmlTag> _tags = new List<HtmlTag>();

            [Given]
            public void GivenHtmlContainsSelfClosingTags()
            {
                _sut = new HtmlParser(SpecHelper.GetFile("Parsing/Files/Self-closing-tags.html"));
            }

            [When]
            public void WhenTheHtmlIsParsed()
            {
                while (_sut.ParseNext(
                    new[]
                    {
                        "pagelet"
                    },
                    out var tag))
                {
                    _tags.Add(tag);
                }
            }

            [Then]
            public void ThenTheTagsAreExtracted()
            {
                Assert.That(_tags, Has.Count.EqualTo(3));
                ParserAssert.TagAccurate(_tags[0], "pagelet", 26);
                ParserAssert.TagAccurate(_tags[1], "pagelet", 27);
                ParserAssert.TagAccurate(_tags[2], "pagelet", 28);
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        class EndTags
        {
            private HtmlParser _sut;
            private readonly List<HtmlTag> _tags = new List<HtmlTag>();

            [Given]
            public void GivenHtmlContainsTagsWithEndTags()
            {
                _sut = new HtmlParser(SpecHelper.GetFile("Parsing/Files/End-tags.html"));
            }

            [When]
            public void WhenTheHtmlIsParsed()
            {
                while (_sut.ParseNext(
                    new[]
                    {
                        "pagelet"
                    },
                    out var tag))
                {
                    _tags.Add(tag);
                }
            }

            [Then]
            public void ThenTheTagsAreExtracted()
            {
                Assert.That(_tags, Has.Count.EqualTo(3));

                // Different line ending in test files.
                ParserAssert.TagAccurate(_tags[0], "pagelet", 48);
                ParserAssert.TagAccurate(_tags[1], "pagelet", 35);
                ParserAssert.TagAccurate(_tags[2], "pagelet", 38);
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        class NonClosedTags
        {
            private HtmlParser _sut;
            private readonly List<HtmlTag> _tags = new List<HtmlTag>();

            [Given]
            public void GivenHtmlContainsNonClosedTags()
            {
                _sut = new HtmlParser(SpecHelper.GetFile("Parsing/Files/Non-closed-tags.html"));
            }

            [When]
            public void WhenTheHtmlIsParsed()
            {
                while (_sut.ParseNext(
                    new[]
                    {
                        "pagelet"
                    },
                    out var tag))
                {
                    _tags.Add(tag);
                }
            }

            [Then]
            public void ThenTheTagsAreExtracted()
            {
                Assert.That(_tags, Has.Count.EqualTo(2));
                ParserAssert.TagAccurate(_tags[0], "pagelet", 24);
                ParserAssert.TagAccurate(_tags[1], "pagelet", 25);
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        class NoClosingBracket
        {
            private HtmlParser _sut;
            private readonly List<HtmlTag> _tags = new List<HtmlTag>();

            [Given]
            public void GivenHtmlContainsATagWithoutAClosingBracket()
            {
                _sut = new HtmlParser(SpecHelper.GetFile("Parsing/Files/Invalid-tag-with-no-closing-bracket.html"));
            }

            [When]
            public void WhenTheHtmlIsParsed()
            {
                while (_sut.ParseNext(
                    new[]
                    {
                        "pagelet"
                    },
                    out var tag))
                {
                    _tags.Add(tag);
                }
            }

            [Then]
            public void ThenTheTagIsIgnored()
            {
                Assert.That(_tags, Has.Count.EqualTo(0));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        class LastTagWithNoClosingBracket
        {
            private HtmlParser _sut;
            private readonly List<HtmlTag> _tags = new List<HtmlTag>();

            [Given]
            public void GivenHtmlContainsAnEndTagWithoutAClosingBracket()
            {
                _sut = new HtmlParser(
                    SpecHelper.GetFile("Parsing/Files/Invalid-last-tag-with-no-closing-bracket.html"));
            }

            [When]
            public void WhenTheHtmlIsParsed()
            {
                while (_sut.ParseNext(
                    new[]
                    {
                        "pagelet"
                    },
                    out var tag))
                {
                    _tags.Add(tag);
                }
            }

            [Then]
            public void ThenTheTagIsIgnored()
            {
                Assert.That(_tags, Has.Count.EqualTo(0));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }

        class InvalidAttributes
        {
            private HtmlParser _sut;
            private readonly List<HtmlTag> _tags = new List<HtmlTag>();

            [Given]
            public void GivenHtmlContainsATagWithInvalidAttributes()
            {
                _sut = new HtmlParser(SpecHelper.GetFile("Parsing/Files/Tag-with-invalid-attributes.html"));
            }

            [When]
            public void WhenTheHtmlIsParsed()
            {
                while (_sut.ParseNext(
                    new[]
                    {
                        "pagelet"
                    },
                    out var tag))
                {
                    _tags.Add(tag);
                }
            }

            [Then]
            public void ThenTheTagIsIgnored()
            {
                Assert.That(_tags, Has.Count.EqualTo(0));
            }

            [Test]
            public void Run()
            {
                this.BDDfy();
            }
        }
    }
}