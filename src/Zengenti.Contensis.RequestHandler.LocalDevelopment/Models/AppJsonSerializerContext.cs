using System.Text.Json.Serialization;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<Block>))]
[JsonSerializable(typeof(List<Renderer>))]
[JsonSerializable(typeof(List<Proxy>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Block))]
[JsonSerializable(typeof(Renderer))]
[JsonSerializable(typeof(Proxy))]
[JsonSerializable(typeof(Endpoint))]
[JsonSerializable(typeof(EndpointRef))]
[JsonSerializable(typeof(RendererRule))]
[JsonSerializable(typeof(AuthenticationResponse))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}