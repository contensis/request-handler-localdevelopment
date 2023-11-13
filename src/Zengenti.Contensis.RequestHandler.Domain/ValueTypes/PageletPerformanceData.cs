using System.Net.Http.Headers;

namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public class PageletPerformanceData
{
    private readonly HttpRequestHeaders _requestHeaders;
    private readonly Uri? _requestUri;
    private readonly HttpMethod _requestMethod;

    public PageletPerformanceData(
        long requestMs,
        long parsingMs,
        long durationMs,
        Uri? requestUri,
        HttpMethod requestMethod,
        HttpRequestHeaders requestHeaders)
    {
        RequestMs = requestMs;
        ParsingMs = parsingMs;
        DurationMs = durationMs;
        _requestHeaders = requestHeaders;
        _requestUri = requestUri;
        _requestMethod = requestMethod;
    }

    public PageletPerformanceData(
        PageletPerformanceMeasurer measurer,
        Uri? requestUri,
        HttpMethod requestMethod,
        HttpRequestHeaders requestHeaders)
        : this(measurer.RequestMs, measurer.ParsingMs, measurer.DurationMs, requestUri, requestMethod, requestHeaders)
    {
    }

    private long RequestMs { get; }

    private long ParsingMs { get; }

    private long DurationMs { get; }

    public override string ToString()
    {
        var separator = Environment.NewLine;
        var headers = string.Join(
            "",
            _requestHeaders.Select(h => $" -H \"{h.Key}: {string.Join("; ", h.Value)}\""));

        return $"<!-- {separator} durationMs = {DurationMs} {separator} requestMs = {RequestMs} {separator} " +
               $"parsingMs = {ParsingMs} {separator} curl{headers} --request {_requestMethod.Method} " +
               $"{_requestUri?.AbsoluteUri} {separator} -->{separator}";
    }
}