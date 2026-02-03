using System.Text.Json.Serialization;
using Zengenti.Contensis.RequestHandler.Domain.Entities;
using Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

namespace Zengenti.Contensis.RequestHandler.Domain;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(RouteInfo))]
[JsonSerializable(typeof(RequestContext))]
[JsonSerializable(typeof(BlockVersionInfo))]
[JsonSerializable(typeof(Headers))]
[JsonSerializable(typeof(ProxyInfo))]
[JsonSerializable(typeof(EndpointExceptionData))]
[JsonSerializable(typeof(List<object>))]
[JsonSerializable(typeof(Dictionary<string, IEnumerable<string>>))]
public partial class DomainJsonSerializerContext : JsonSerializerContext
{
}