using System.Text.Json.Serialization;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Application;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<object>))]
[JsonSerializable(typeof(EndpointExceptionData))]
[JsonSerializable(typeof(RouteInfo))]
[JsonSerializable(typeof(BlockVersionInfo))]
[JsonSerializable(typeof(Headers))]
[JsonSerializable(typeof(ProxyInfo))]
[JsonSerializable(typeof(Dictionary<string, IEnumerable<string>>))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}