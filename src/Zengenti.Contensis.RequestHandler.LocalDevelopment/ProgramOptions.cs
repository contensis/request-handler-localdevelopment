using CommandLine;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment;

public class ProgramOptions
{
    public static ProgramOptions Current { get; private set; } = null!;
    
    public ProgramOptions()
    {
        Current = this;
    }
    
    
    [Option('p', longName: "port", HelpText = "The port number that will be used by the application.", Default = 5000)]
    public int? Port { get; init; }
    
    [Option('c', longName: "configFile", HelpText = "The path to the site config file.")]
    public string? ConfigFile { get; init; }
    
    [Option("alias", HelpText = "The alias for the site config.")]
    public string? Alias { get; init; }
    
    [Option("project-id", HelpText = "The API id of the project for the site config.")]
    public string? ProjectId { get; init; }
    
    [Option("access-token", HelpText = "The access token for the site config.")]
    public string? AccessToken { get; init; }
    
    [Option("client-id", HelpText = "The client id for the site config.")]
    public string? ClientId { get; init; }
    
    [Option("client-secret", HelpText = "The client secret for the site config.")]
    public string? ClientSecret { get; init; }
    
    [Option("blocks-json", HelpText = "The blocks as JSON for the site config.")]
    public string? BlocksAsJson { get; init; }
    
    [Option("renderers-json", HelpText = "The renderers as JSON for the site config.")]
    public string? RenderersAsJson { get; init; }
}