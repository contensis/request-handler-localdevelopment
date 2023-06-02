namespace Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Blocks;

public class BlockVersionDataForUi
{
    public BlockVersionDataForUi(
        string pushedBy,
        DateTime pushed,
        string releasedBy,
        DateTime? released,
        string markedBrokenBy,
        DateTime? markedBroken,
        DateTime? madeLive,
        string madeLiveBy,
        int versionNo)
    {
        PushedBy = pushedBy;
        Pushed = pushed;
        ReleasedBy = releasedBy;
        Released = released;
        MarkedBrokenBy = markedBrokenBy;
        MarkedBroken = markedBroken;
        MadeLive = madeLive;
        MadeLiveBy = madeLiveBy;
        VersionNo = versionNo;
    }

    public string PushedBy { get; }
    public DateTime Pushed { get; }
    public string ReleasedBy { get; }
    public DateTime? Released { get; }
    public string MarkedBrokenBy { get; }
    public DateTime? MarkedBroken { get; }
    public DateTime? MadeLive { get; }
    public string MadeLiveBy { get; }
    public int VersionNo { get; }
}