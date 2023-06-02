using System.Diagnostics;

namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public class PageletPerformanceMeasurer
{
    private Stopwatch _stopWatch;
    private readonly bool _traceEnabled;

    public PageletPerformanceMeasurer(bool traceEnabled, bool autoStart = false)
    {
        _traceEnabled = traceEnabled;
        _stopWatch = new Stopwatch();

        if (autoStart)
        {
            Begin();
        }
    }

    public long DurationMs { get; private set; }

    public long ParsingMs { get; private set; }

    public long RequestMs { get; private set; }

    private void Begin()
    {
        ParsingMs = 0;
        RequestMs = 0;
        DurationMs = 0;
        if (!_traceEnabled)
        {
            return;
        }
        _stopWatch = Stopwatch.StartNew();
    }

    public void EndOfParsing()
    {
        if (!_traceEnabled)
        {
            return;
        }

        _stopWatch.Stop();

        ParsingMs = _stopWatch.ElapsedMilliseconds;

        _stopWatch = Stopwatch.StartNew();
    }

    public void EndOfRequest()
    {
        if (!_traceEnabled)
        {
            return;
        }

        _stopWatch.Stop();
        RequestMs = _stopWatch.ElapsedMilliseconds;
        _stopWatch = Stopwatch.StartNew();
    }

    public void End()
    {
        if (!_traceEnabled)
        {
            return;
        }

        _stopWatch.Stop();
        DurationMs = ParsingMs + RequestMs + _stopWatch.ElapsedMilliseconds;
    }
}