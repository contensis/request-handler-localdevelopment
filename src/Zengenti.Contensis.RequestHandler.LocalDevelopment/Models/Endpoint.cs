using System.Text.Json.Serialization;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

public class Endpoint
{
    public string Id { get; set; } = null!;

    public string Path { get; set; } = null!;

    public bool UseOriginPathAndQuery { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    
    [JsonIgnore]
    public Block Block { get; set; } = null!;

    public Uri Uri => new Uri(Block.BaseUri!, Path);
}