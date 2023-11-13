namespace Zengenti.Contensis.RequestHandler.Domain.Entities;

/// <summary>
///     A light-weight object to hold the details of a block version required for invoking static resource endpoints.
/// </summary>
public class BlockVersionInfo
{
    public BlockVersionInfo(
        Guid projectUuid,
        string blockId,
        Guid blockVersionId,
        Uri baseUri,
        string branch,
        bool enableFullUriRouting,
        IEnumerable<string>? staticPaths = null,
        int? versionNo = null)
    {
        ProjectUuid = projectUuid;
        BlockId = blockId;
        BlockVersionId = blockVersionId;
        BaseUri = baseUri;
        EnableFullUriRouting = enableFullUriRouting;
        Branch = branch;
        StaticPaths = staticPaths?.ToList() ?? new List<string>();
        VersionNo = versionNo;
    }

    public Guid ProjectUuid { get; }
    public string BlockId { get; }
    public Guid BlockVersionId { get; }
    public Uri BaseUri { get; }
    public bool EnableFullUriRouting { get; }
    public List<string> StaticPaths { get; }
    public int? VersionNo { get; }
    public string Branch { get; }
}