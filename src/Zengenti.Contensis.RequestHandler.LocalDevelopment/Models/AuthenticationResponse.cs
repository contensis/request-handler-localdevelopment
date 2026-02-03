using System.Text.Json.Serialization;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

/// <summary>
///     Represents the response from the authentication endpoint
/// </summary>
public class AuthenticationResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
}