namespace Zengenti.Contensis.RequestHandler.Application.Parsing;

public class HtmlTag
{
    public HtmlTag(string name, int startPos)
    {
        Name = name;
        StartPos = startPos;
    }

    /// <summary>
    /// Transient tag identifier.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Name of this tag.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The start position of the tag in the document.
    /// </summary>
    public int StartPos { get; private set; }

    /// <summary>
    /// The end position of the tag in the document.
    /// </summary>
    public int EndPos { get; set; }

    /// <summary>
    /// Collection of attribute names and values for this tag
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// True if this tag contained a trailing forward slash
    /// </summary>
    public bool SelfClosing { get; set; }

    /// <summary>
    /// Indicates if this tag contains the specified attribute. Note that
    /// true is returned when this tag contains the attribute even when the
    /// attribute has no value
    /// </summary>
    /// <param name="name">Name of attribute to check</param>
    /// <returns>True if tag contains attribute or false otherwise</returns>
    public bool HasAttribute(string name)
    {
        return Attributes.ContainsKey(name);
    }

    /// <summary>
    /// Moves the position of the tag start and end values by an offset.
    /// </summary>
    /// <param name="offset">The amount to move the location values of the tag</param>
    public void Move(int offset)
    {
        StartPos += offset;
        EndPos += offset;
    }

    /// <summary>
    /// Gets the length of the tag
    /// </summary>
    public int Length => EndPos - StartPos;

    public override string ToString()
    {
        var trail = SelfClosing ? " /" : "";
        var attributesString = string.Join(" ", Attributes.Select(x => $"{x.Key}=\"{x.Value}\""));
        var markup = $"<{Name} {attributesString}{trail}>";

        return markup;
    }
}