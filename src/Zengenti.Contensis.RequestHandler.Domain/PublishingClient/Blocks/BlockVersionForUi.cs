namespace Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Blocks;

public class BlockVersionForUi
{
    public BlockVersionForUi(SourceInfo source, BlockStatusInfo status, BlockVersionDataForUi version, bool active,
        bool @default)
    {
        Source = source;
        Status = status;
        Version = version;
        Active = active;
        Default = @default;
    }

    public SourceInfo Source { get; }

    public BlockStatusInfo Status { get; }

    public BlockVersionDataForUi Version { get; }

    public bool Active { get; }

    public bool Default { get; }
}