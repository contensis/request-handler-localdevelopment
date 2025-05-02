using CommandLine;

namespace Zengenti.Contensis.RequestHandler.LocalDevelopment;

public class Program
{
    private static void Main(string[] args)
    {
        var parser = new Parser(with => { with.IgnoreUnknownArguments = true; });

        parser.ParseArguments<ProgramOptions>(args)
            .WithParsed(opts => { CreateHostBuilder(args, opts).Build().Run(); })
            .WithNotParsed(HandleParseError);
    }

    public static IHostBuilder CreateHostBuilder(string[] args, ProgramOptions opts) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .UseStartup<Startup>()
                    .UseUrls($"http://*:{opts.Port}")
                    .UseKestrel(options =>
                    {
                        options.AddServerHeader = false;
                        options.Limits.MaxRequestBodySize = 5368709120;
                    });
            });

    private static void HandleParseError(IEnumerable<Error> errors)
    {
        var errorMessages = new List<string>();
        foreach (var error in errors)
        {
            switch (error)
            {
                case NamedError namedError:
                    errorMessages.Add(
                        $"Unable to parse `--{namedError.NameInfo.LongName}` with error `{namedError.Tag}`");
                    break;
                default:
                    errorMessages.Add($"Unable to parse non-named argument with error `{error.Tag}`");
                    break;
            }
        }

        if (errorMessages.Count == 0)
        {
            return;
        }

        Console.WriteLine(string.Join("\n", errorMessages));

        Environment.Exit(1);
    }
}