namespace Zengenti.Contensis.RequestHandler.Domain.Entities;

/// <summary>
///     A light-weight object to hold the details of a block version required for invoking static resource endpoints.
/// </summary>
public class BlockVersionInfo(
    Guid projectUuid,
    string blockId,
    Guid blockVersionId,
    Uri baseUri,
    string branch,
    bool enableFullUriRouting,
    DateTime pushed,
    IEnumerable<string>? staticPaths = null,
    int? versionNo = null)
{
    public Guid ProjectUuid { get; } = projectUuid;

    public string BlockId { get; } = blockId;

    public Guid BlockVersionId { get; } = blockVersionId;

    public Uri BaseUri { get; } = baseUri;

    public string Branch { get; } = branch;

    public bool EnableFullUriRouting { get; } = enableFullUriRouting;

    public DateTime Pushed { get; } = pushed;

    public List<string> StaticPaths { get; } = staticPaths?.ToList() ?? new List<string>();

    public int? VersionNo { get; } = versionNo;
}