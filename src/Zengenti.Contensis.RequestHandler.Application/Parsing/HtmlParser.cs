namespace Zengenti.Contensis.RequestHandler.Application.Parsing;

public class HtmlParser : TextParser
{
    private const string EndComment = "-->";

    public HtmlParser(string html) : base(html)
    {
    }

    /// <summary>
    ///     Parses the next tag that matches the specified tag name
    /// </summary>
    /// <param name="name">Name of the tags to parse ("*" = parse all tags)</param>
    /// <param name="tag">
    ///     Returns information on the next occurrence of the
    ///     specified tag or null if none found
    /// </param>
    /// <returns>True if a tag was parsed or false if the end of the document was reached</returns>
    // ReSharper disable once CognitiveComplexity
    public bool ParseNext(string[] name, out HtmlTag tag)
    {
        // Must always set out parameter
        tag = null!;

        // Nothing to do if no tag specified
        if (name.Length == 0)
            return false;

        // Loop until match is found or no more tags
        MoveTo('<');

        var pos = Position;

        while (!EndOfText)
        {
            // Skip over opening '<'
            MoveAhead();

            // Examine first tag character
            var c = Peek();

            if (c == '!' && Peek(1) == '-' && Peek(2) == '-')
            {
                // Skip over comments
                MoveTo(EndComment);
                MoveAhead(EndComment.Length);
            }
            else if (c == '/')
            {
                // Skip over closing tags
                MoveTo('>');
                MoveAhead();
            }
            else
            {
                // Parse tag
                var result = ParseTag(name, pos, ref tag!, out var inScript);

                // Because scripts may contain tag characters, we have special
                // handling to skip over script contents
                if (inScript)
                {
                    MovePastScript();
                }

                // Return true if requested tag was found
                if (result)
                {
                    return true;
                }
            }

            // Find next tag
            MoveTo('<');
            pos = Position;
        }

        // No more matching tags found
        return false;
    }

    /// <summary>
    ///     Parses the contents of an HTML tag. The current position should be at the first
    ///     character following the tag's opening less-than character.
    ///     Note: We parse to the end of the tag even if this tag was not requested by the
    ///     caller. This ensures subsequent parsing takes place after this tag
    /// </summary>
    /// <param name="reqName">
    ///     Name of the tag the caller is requesting, or "*" if caller
    ///     is requesting all tags
    /// </param>
    /// <param name="startPos"></param>
    /// <param name="tag">
    ///     Returns information on this tag if it's one the caller is
    ///     requesting
    /// </param>
    /// <param name="inScript">
    ///     Returns true if tag began, and did not end, and script
    ///     block
    /// </param>
    /// <returns>
    ///     True if data is being returned for a tag requested by the caller
    ///     or false otherwise
    /// </returns>
    // ReSharper disable once CognitiveComplexity
    private bool ParseTag(string[] reqName, int startPos, ref HtmlTag? tag, out bool inScript)
    {
        var doctype = false;
        var requested = false;
        inScript = false;

        // Get name of this tag
        var name = ParseTagName().ToString();

        // Special handling
        if (name.Equals("!DOCTYPE", StringComparison.OrdinalIgnoreCase))
        {
            doctype = true;
        }
        else if (name.Equals("script", StringComparison.OrdinalIgnoreCase))
        {
            inScript = true;
        }

        // Is this a tag requested by caller?
        //if (reqName == "*" ||
        if (reqName.ContainsCaseInsensitive(name))
        {
            requested = true;
            tag = new HtmlTag(name, startPos);
        }

        // Parse attributes
        MovePastWhitespace();

        while (Peek() != '>' && Peek() != NullChar)
        {
            if (Peek() == '/')
            {
                // Handle trailing forward slash
                if (requested)
                {
                    tag!.SelfClosing = true;
                }

                MoveAhead();
                MovePastWhitespace();

                // If this is a script tag, it was closed
                inScript = false;
            }
            else if (Peek() == '<')
            {
                tag = null;
                return false;
            }
            else
            {
                // Parse attribute name
                name = !doctype ? ParseAttributeName() : ParseAttributeValue();
                MovePastWhitespace();

                // Parse attribute value
                var value = string.Empty;
                if (Peek() == '=')
                {
                    MoveAhead();
                    MovePastWhitespace();
                    value = ParseAttributeValue();
                    MovePastWhitespace();
                }

                // Add attribute to collection if requested tag
                if (requested)
                {
                    // This tag replaces existing tags with same name
                    if (tag!.Attributes.ContainsKey(name))
                    {
                        tag.Attributes.Remove(name);
                    }

                    tag.Attributes.Add(name, value);
                }
            }
        }

        if (tag != null)
        {
            // Skip over closing '>'
            MoveAhead();

            tag.EndPos = Position;

            if (!tag.SelfClosing)
            {
                MovePastWhitespace();

                // Handle a mal-formed tag at the end of content
                if (EndOfText)
                {
                    tag = null;
                    return false;
                }

                // Add the closed tag
                // At the moment this does not handle a tag with any content
                var endTagEndPos = Position + tag.Name.Length + 2;

                if (Substring(Position, endTagEndPos).Equals($"</{tag.Name}", StringComparison.OrdinalIgnoreCase))
                {
                    MoveAhead(tag.Name.Length + 2);
                    MoveTo('>');
                    MoveAhead();
                    tag.EndPos = Position;
                }
            }
        }

        return requested;
    }

    /// <summary>
    ///     Parses a tag name. The current position should be the first character of the name
    /// </summary>
    /// <returns>Returns the parsed name string</returns>
    private ReadOnlySpan<char> ParseTagName()
    {
        var start = Position;

        while (!EndOfText && !char.IsWhiteSpace(Peek()) && Peek() != '>')
        {
            MoveAhead();
        }

        return Substring(start, Position);
    }

    /// <summary>
    ///     Parses an attribute name. The current position should be the first character
    ///     of the name
    /// </summary>
    /// <returns>Returns the parsed name string</returns>
    private string ParseAttributeName()
    {
        var start = Position;

        while (!EndOfText && !char.IsWhiteSpace(Peek()) && Peek() != '>' && Peek() != '=')
        {
            MoveAhead();
        }

        return Substring(start, Position).ToString();
    }

    /// <summary>
    ///     Parses an attribute value. The current position should be the first non-whitespace
    ///     character following the equal sign.
    ///     Note: We terminate the name or value if we encounter a new line. This seems to
    ///     be the best way of handling errors such as values missing closing quotes, etc.
    /// </summary>
    /// <returns>Returns the parsed value string</returns>
    private string ParseAttributeValue()
    {
        int start, end;
        var c = Peek();

        if (c == '"' || c == '\'')
        {
            // Move past opening quote
            MoveAhead();

            // Parse quoted value
            start = Position;
            MoveTo(
                new[]
                {
                    c,
                    '\r',
                    '\n'
                });
            end = Position;

            // Move past closing quote
            if (Peek() == c)
            {
                MoveAhead();
            }
        }
        else
        {
            // Parse unquoted value
            start = Position;
            while (!EndOfText && !char.IsWhiteSpace(c) && c != '>')
            {
                MoveAhead();
                c = Peek();
            }

            end = Position;
        }

        return Substring(start, end).ToString();
    }

    /// <summary>
    ///     Locates the end of the current script and moves past the closing tag
    /// </summary>
    private void MovePastScript()
    {
        const string endScript = "</script";

        while (!EndOfText)
        {
            MoveTo(endScript, true);
            MoveAhead(endScript.Length);

            if (Peek() == '>' || char.IsWhiteSpace(Peek()))
            {
                MoveTo('>');
                MoveAhead();
                break;
            }
        }
    }
}