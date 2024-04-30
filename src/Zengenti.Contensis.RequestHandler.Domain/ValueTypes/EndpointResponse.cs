namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

/// <summary>
///     The response from a EndpointRequestService
/// </summary>
public class EndpointResponse
{
    public EndpointResponse(
        string? content,
        Dictionary<string, IEnumerable<string>> headers,
        int statusCode,
        PageletPerformanceData? pageletPerformanceData = null)
    {
        StringContent = content;
        Headers = headers;
        StatusCode = statusCode;

        if (pageletPerformanceData != null)
        {
            PageletPerformanceData = pageletPerformanceData;
        }
    }

    public EndpointResponse(
        Stream content,
        Dictionary<string, IEnumerable<string>> headers,
        int statusCode,
        PageletPerformanceData? pageletPerformanceData = null)
    {
        StreamContent = content;
        Headers = headers;
        StatusCode = statusCode;

        if (pageletPerformanceData != null)
        {
            PageletPerformanceData = pageletPerformanceData;
        }
    }

    public string? StringContent { get; }

    public Dictionary<string, IEnumerable<string>> Headers { get; }

    public int StatusCode { get; set; }

    private Stream? StreamContent { get; }

    public PageletPerformanceData? PageletPerformanceData { get; }

    public Stream? ToStream(bool resetPosition = false)
    {
        if (StreamContent != null)
        {
            if (resetPosition)
            {
                StreamContent.Position = 0;
            }

            return StreamContent;
        }

        if (string.IsNullOrWhiteSpace(StringContent))
        {
            return null;
        }

        var stream = new MemoryStream();

        using var writer = new StreamWriter(stream, leaveOpen: true);
        writer.Write(StringContent);
        writer.Flush();
        if (resetPosition)
        {
            stream.Position = 0;
        }

        return stream;
    }

    public bool IsSuccessStatusCode()
    {
        return StatusCode >= 200 && StatusCode < 300;
    }

    public bool IsErrorStatusCode()
    {
        return StatusCode >= 400;
    }
}