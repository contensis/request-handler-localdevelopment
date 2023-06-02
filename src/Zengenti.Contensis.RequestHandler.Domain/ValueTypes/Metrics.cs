namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public class Metrics
{
    private readonly Dictionary<string, long> _metrics = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string name, long duration)
    {
        _metrics[name] = duration;
    }

    public override string ToString()
    {
        return string.Join(", ", _metrics.Select(x => x.Key + ": " + x.Value + "ms").ToArray());
    }
}