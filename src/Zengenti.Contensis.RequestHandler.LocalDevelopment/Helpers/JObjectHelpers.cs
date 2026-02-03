using Newtonsoft.Json.Linq;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Helpers;

public static class JObjectHelpers
{
    public static JObject? GetObject(JObject obj, string propertyName)
    {
        return obj.TryGetValue(propertyName, out var node) ? node as JObject : null;
    }

    public static string? GetString(JObject obj, string propertyName)
    {
        if (!obj.TryGetValue(propertyName, out var node) || node.Type == JTokenType.Null)
        {
            return null;
        }

        return node.Type == JTokenType.String ? node.Value<string>() : node.ToString();
    }

    public static bool? GetBool(JObject obj, string propertyName)
    {
        if (!obj.TryGetValue(propertyName, out var node) || node.Type == JTokenType.Null)
        {
            return null;
        }

        if (node.Type == JTokenType.Boolean)
        {
            return node.Value<bool>();
        }

        var text = node.ToString();
        return bool.TryParse(text, out var result) ? result : null;
    }

    public static Guid? GetGuid(JObject obj, string propertyName)
    {
        var value = GetString(obj, propertyName);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}
