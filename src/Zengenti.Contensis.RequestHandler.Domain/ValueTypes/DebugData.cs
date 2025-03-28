namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public class DebugData(RouteInfo routeInfo)
{
    private readonly Dictionary<string, object?> _debugData = new(StringComparer.OrdinalIgnoreCase);
    private DebugData? _initialDebugData;

    public AppConfiguration? AppConfiguration { get; set; }
    public NodeInfo? NodeInfo { get; set; }

    public string NodeCheckResult { get; set; } = "";

    public string EndpointError { get; set; } = "";
    public string EndpointErrorCurl { get; set; } = "";

    public DebugData? InitialDebugData
    {
        get => _initialDebugData;
        set
        {
            _initialDebugData = value;
            if (_initialDebugData != null)
            {
                _initialDebugData.AppConfiguration = null;
            }
        }
    }

    public override string ToString()
    {
        _debugData.Add("NodeInfo", NodeInfo);
        if (!string.IsNullOrWhiteSpace(NodeCheckResult))
        {
            _debugData.Add("NodeCheckResult", NodeCheckResult);
        }

        _debugData.Add("RouteType", routeInfo.RouteType.ToString());
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        _debugData.Add("Uri", routeInfo.Uri?.ToString() ?? "");
        _debugData.Add("BlockVersionInfo", routeInfo.BlockVersionInfo);

        if (!string.IsNullOrWhiteSpace(EndpointError))
        {
            _debugData.Add("EndpointError", EndpointError);
        }

        if (!string.IsNullOrWhiteSpace(EndpointErrorCurl))
        {
            _debugData.Add("EndpointErrorCurl", EndpointErrorCurl);
        }

        if (AppConfiguration != null)
        {
            _debugData.Add("AppConfiguration", AppConfiguration);
        }

        return _debugData.ToJson();
    }
}