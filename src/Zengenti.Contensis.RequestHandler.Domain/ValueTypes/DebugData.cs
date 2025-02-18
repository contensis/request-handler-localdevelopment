using Zengenti.Contensis.RequestHandler.Domain.Entities;

namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

public class DebugData(RouteInfo routeInfo)
{
    private readonly Dictionary<string, object?> _debugData = new(StringComparer.OrdinalIgnoreCase);

    public Node? Node { get; set; }

    public AppConfiguration? AppConfiguration { get; set; }

    public override string ToString()
    {
        _debugData.Add("Node", Node);
        _debugData.Add("FoundRoute", routeInfo.FoundRoute);
        _debugData.Add("IsIisFallback", routeInfo.IsIisFallback);
        _debugData.Add("BlockVersionInfo", routeInfo.BlockVersionInfo);
        _debugData.Add("Uri", routeInfo.Uri.ToString());
        _debugData.Add("AppConfiguration", AppConfiguration);

        return _debugData.ToJson();
    }
}