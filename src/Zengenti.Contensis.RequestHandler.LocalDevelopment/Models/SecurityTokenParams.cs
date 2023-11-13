namespace Zengenti.Contensis.RequestHandler.LocalDevelopment.Models;

public record SecurityTokenParams(
    string Alias,
    string? ClientId = null,
    string? ClientSecret = null,
    string? Username = null,
    string? Password = null);