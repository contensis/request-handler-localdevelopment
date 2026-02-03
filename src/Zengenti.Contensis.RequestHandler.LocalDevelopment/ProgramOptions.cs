using System.Diagnostics.CodeAnalysis;
using CommandLine;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment;

// ReSharper disable once ClassNeverInstantiated.Global
[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public class ProgramOptions
{
    public static ProgramOptions Current { get; private set; } = null!;

    public ProgramOptions()
    {
        Current = this;
    }

    [Option('p', longName: "port", HelpText = "The port number that will be used by the application.", Default = 5000)]
    public int? Port { get; set; }

    [Option('c', longName: "config-file", HelpText = "The path to the site config file.")]
    public string? ConfigFile { get; set; }

    [Option("alias", HelpText = "The alias for the site config.")]
    public string? Alias { get; set; }

    [Option("project-api-id", HelpText = "The API id of the project for the site config.")]
    public string? ProjectApiId { get; set; }

    [Option("access-token", HelpText = "The access token for the site config.")]
    public string? AccessToken { get; set; }

    [Option("client-id", HelpText = "The client id for the site config (when username and password are not specified)")]
    public string? ClientId { get; set; }

    [Option(
        "client-secret",
        HelpText = "The client secret for the site config (when username and password are not specified)")]
    public string? ClientSecret { get; set; }

    [Option("username", HelpText = "The username for the site config (when client id and secret are not specified)")]
    public string? Username { get; set; }

    [Option("password", HelpText = "The password for the site config (when client id and secret are not specified)")]
    public string? Password { get; set; }

    [Option("blocks-json", HelpText = "The blocks as JSON for the site config.")]
    public string? BlocksAsJson { get; set; }

    [Option("renderers-json", HelpText = "The renderers as JSON for the site config.")]
    public string? RenderersAsJson { get; set; }

    [Option("iis-hostname", HelpText = "The IIS hostname for the site config.")]
    public string? IisHostname { get; set; }

    [Option("pod-cluster-id", HelpText = "The pod cluster id", Default = "hq")]
    public string? PodClusterId { get; set; }

    [Option("block-cluster-ingress-ip", HelpText = "The ingress IP for the block cluster", Default = null)]
    public string? BlockClusterIngressIp { get; set; }

    public string BlockAddressSuffix => "blocks.contensis.com";
}