using Zengenti.Contensis.RequestHandler.Domain.Entities;

namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public class DebugData(RouteInfo routeInfo)
{
    private readonly Dictionary<string, object?> _debugData = new(StringComparer.OrdinalIgnoreCase);
    private DebugData? _initialDebugData;

    public AppConfiguration? AppConfiguration { get; set; }
    public Node? Node { get; set; }

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
        _debugData.Add("Node", Node);
        if (!string.IsNullOrWhiteSpace(NodeCheckResult))
        {
            _debugData.Add("NodeCheckResult", NodeCheckResult);
        }

        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        _debugData.Add("Uri", routeInfo.Uri?.ToString() ?? "");
        _debugData.Add("FoundRoute", routeInfo.FoundRoute);
        _debugData.Add("BlockVersionInfo", routeInfo.BlockVersionInfo);
        _debugData.Add("IsIisFallback", routeInfo.IsIisFallback);

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