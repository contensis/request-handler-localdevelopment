namespace Zengenti.Contensis.RequestHandler.Application.Parsing;

public class TextParser
{
    private readonly string _text;
    private int _pos;

    protected int Position => _pos;
    protected int Remaining => _text.Length - _pos;

    protected const char NullChar = (char)0;

    protected TextParser(string text)
    {
        _text = text;
    }

    /// <summary>
    /// Indicates if the current position is at the end of the current document
    /// </summary>
    protected bool EndOfText => (_pos >= _text.Length);

    /// <summary>
    /// Returns the character at the specified number of characters beyond the current
    /// position, or a null character if the specified position is at the end of the
    /// document
    /// </summary>
    /// <param name="ahead">The number of characters beyond the current position</param>
    /// <returns>The character at the specified position</returns>
    protected char Peek(int ahead = 0)
    {
        var pos = (_pos + ahead);

        if (pos < _text.Length)
        {
            return _text[pos];
        }

        return NullChar;
    }

    /// <summary>
    /// Extracts a substring from the specified position to the end of the text
    /// </summary>
    /// <param name="start"></param>
    /// <returns></returns>
    protected ReadOnlySpan<char> Substring(int start)
    {
        return Substring(start, _text.Length);
    }

    /// <summary>
    /// Extracts a substring from the specified range of the current text
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    protected ReadOnlySpan<char> Substring(int start, int end)
    {
        return _text.AsSpan()[start..end];
    }

    /// <summary>
    /// Moves the current position ahead the specified number of characters
    /// </summary>
    /// <param name="ahead">The number of characters to move ahead</param>
    protected void MoveAhead(int ahead = 1)
    {
        _pos = Math.Min(_pos + ahead, _text.Length);
    }

    /// <summary>
    /// Moves to the next occurrence of the specified string
    /// </summary>
    /// <param name="s">String to find</param>
    /// <param name="ignoreCase">Indicates if case-insensitive comparisons are used</param>
    protected void MoveTo(string s, bool ignoreCase = false)
    {
        _pos = _text.IndexOf(s, _pos,
            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        if (_pos < 0)
        {
            _pos = _text.Length;
        }
    }

    /// <summary>
    /// Moves to the next occurrence of the specified character
    /// </summary>
    /// <param name="c">Character to find</param>
    protected void MoveTo(char c)
    {
        _pos = _text.IndexOf(c, _pos);

        if (_pos < 0)
        {
            _pos = _text.Length;
        }
    }

    /// <summary>
    /// Moves to the next occurrence of any one of the specified
    /// characters
    /// </summary>
    /// <param name="carr">Array of characters to find</param>
    protected void MoveTo(char[] carr)
    {
        _pos = _text.IndexOfAny(carr, _pos);
        if (_pos < 0)
        {
            _pos = _text.Length;
        }
    }

    /// <summary>
    /// Moves the current position to the first character that is part of a newline
    /// </summary>
    protected void MoveToEndOfLine()
    {
        var c = Peek();
        while (c != '\r' && c != '\n' && !EndOfText)
        {
            MoveAhead();
            c = Peek();
        }
    }

    /// <summary>
    /// Moves the current position to the next character that is not whitespace, returning the whitespace as an element.
    /// </summary>
    protected int MovePastWhitespace()
    {
        var length = 0;
        while (char.IsWhiteSpace(Peek()))
        {
            Peek();
            MoveAhead();
            length++;
        }

        return length;
    }
}