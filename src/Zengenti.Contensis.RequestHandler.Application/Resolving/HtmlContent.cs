using System.Text;
using Zengenti.Contensis.RequestHandler.Application.Parsing;

namespace Zengenti.Contensis.RequestHandler.Application.Resolving;

/// <summary>
///     Wraps a mutable instance of HTML content and controls updates to pagelet tags.
/// </summary>
public class HtmlContent
{
    private readonly StringBuilder _content;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    private readonly List<HtmlTag> _tags = new List<HtmlTag>();
    private readonly ILogger _logger;

    public HtmlContent(string html, ILogger logger)
    {
        _content = new StringBuilder(html);
        _logger = logger;
    }

    public void AddTagOffset(HtmlTag tag)
    {
        _lock.Wait();
        try
        {
            _tags.Add(tag);
        }
        finally
        {
            _lock.Release();
        }
    }

    public override string ToString()
    {
        return _content.ToString();
    }

    public async Task UpdateTag(Guid tagId, string? tagContent)
    {
        await _lock.WaitAsync();
        try
        {
            var tag = _tags.SingleOrDefault(t => t.Id == tagId);
            if (tag == null)
            {
                // Tag is unknown
                return;
            }

            if (tagContent == null)
            {
                // Clear the pagelet tag if no response
                tagContent = string.Empty;
            }

            // Replace pagelet tag with resolved content
            _content.Remove(tag.StartPos, tag.Length);
            _content.Insert(tag.StartPos, tagContent);

            var unresolvedLength = tag.Length;
            tag.EndPos = tag.StartPos + tagContent.Length;

            AdjustSubsequentTagPositions(_tags.IndexOf(tag) + 1, tagContent.Length - unresolvedLength);
        }
        catch (Exception e)
        {
            // Potentially a miscalculated offset
            _logger.LogWarning(
                e,
                "Error updating tag in HtmlContent - tag: {Tag}, content: {Content}",
                tagId,
                tagContent);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WrapWithLayout(string layoutContent, int contentTagStartPos, int contentTagEndPos)
    {
        await _lock.WaitAsync();
        try
        {
            var firstPartInsertEndPos = contentTagStartPos + _content.Length;

            _content.Insert(0, layoutContent.Substring(0, contentTagStartPos));
            _content.Insert(firstPartInsertEndPos, layoutContent[contentTagEndPos..]);

            AdjustSubsequentTagPositions(0, contentTagStartPos);
        }
        catch (Exception e)
        {
            // Potentially a miscalculated offset
            _logger.LogWarning(
                e,
                "Error updating layout in HtmlContent - startPos: {StartPos}, endPos: {EndPos}",
                contentTagStartPos,
                contentTagEndPos);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void AdjustSubsequentTagPositions(int startIndex, int amount)
    {
        for (var i = startIndex; i < _tags.Count; i++)
        {
            _tags[i].Move(amount);
        }
    }
}