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

    [Option("project-api-id", HelpText = "The API id of the project for the site config.")]
    public string? ProjectApiId { get; init; }
    
    [Option("access-token", HelpText = "The access token for the site config.")]
    public string? AccessToken { get; init; }

    [Option("client-id", HelpText = "The client id for the site config (when username and password are not specified)")]
    public string? ClientId { get; init; }

    [Option(
        "client-secret",
        HelpText = "The client secret for the site config (when username and password are not specified)")]
    public string? ClientSecret { get; init; }

    [Option("username", HelpText = "The username for the site config (when client id and secret are not specified)")]
    public string? Username { get; init; }

    [Option("password", HelpText = "The password for the site config (when client id and secret are not specified)")]
    public string? Password { get; init; }

    [Option("blocks-json", HelpText = "The blocks as JSON for the site config.")]
    public string? BlocksAsJson { get; init; }

    [Option("renderers-json", HelpText = "The renderers as JSON for the site config.")]
    public string? RenderersAsJson { get; init; }

    [Option("iis-hostname", HelpText = "The IIS hostname for the site config.")]
    public string? IisHostname { get; init; }
    
    [Option("pod-cluster-id", HelpText = "The pod cluster id", Default = null)]
    public string? PodClusterId { get; set; } = null!;
    
    [Option("block-cluster-ingress-ip", HelpText = "The ingress IP for the block cluster", Default = null)]
    public string? BlockClusterIngressIp { get; set; } = null!;

    public string BlockAddressSuffix => "blocks.contensis.com";
}