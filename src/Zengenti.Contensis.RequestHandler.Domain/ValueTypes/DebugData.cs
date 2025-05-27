namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public class DebugData(RouteInfo routeInfo)
{
    private readonly Dictionary<string, object?> _debugData = new(StringComparer.OrdinalIgnoreCase);
    private DebugData? _initialDebugData;
    private DebugData? _additionalDebugData;
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

    public DebugData? AdditionalDebugData
    {
        get => _additionalDebugData;
        set
        {
            _additionalDebugData = value;
            if (_additionalDebugData != null)
            {
                _additionalDebugData.AppConfiguration = null;
            }
        }
    }

    public override string ToString()
    {
        _debugData["NodeInfo"] = NodeInfo;

        if (!string.IsNullOrWhiteSpace(NodeCheckResult))
        {
            _debugData["NodeCheckResult"] = NodeCheckResult;
        }

        _debugData["RouteType"] = routeInfo.RouteType.ToString();
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        _debugData["Uri"] = routeInfo.Uri?.AbsoluteUri ?? "";
        _debugData["BlockVersionInfo"] = routeInfo.BlockVersionInfo;

        if (!string.IsNullOrWhiteSpace(EndpointError))
        {
            _debugData["EndpointError"] = EndpointError;
        }

        if (!string.IsNullOrWhiteSpace(EndpointErrorCurl))
        {
            _debugData["EndpointErrorCurl"] = EndpointErrorCurl;
        }

        if (AppConfiguration != null)
        {
            _debugData["AppConfiguration"] = AppConfiguration;
        }

        return _debugData.ToJson();
    }
}