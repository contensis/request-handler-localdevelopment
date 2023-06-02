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
}