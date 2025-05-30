using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using ZeroQL.CLI.Converters;
using ZeroQL.Config;

namespace ZeroQL.CLI.Commands;

[Command("schema pull", Description = "Pulls the schema from a remote server")]
public class PullSchemaCommand : ICommand
{
    [CommandOption(
        "config",
        'c',
        Description =
            "The generation config file. For example, './zeroql.json'. Use `zeroql config init` to bootstrap.")]
    public string? Config { get; set; }
    
    [CommandOption(
        "url",
        'u',
        Description =
            "The url to pull the schema from. For example, https://server.com/graphql")]
    public Uri? Url { get; set; }
    
    [CommandOption("output", 'o', Description = "The output file. For example, './schema.graphql'")]
    public string Output { get; set; } = "schema.graphql";
    
    [CommandOption("token", 't', Description = "Access Token to use when downloading the schema")]
    public string? AccessToken { get; set; }
    
    [CommandOption("auth", 'a', Description = "Auth scheme to use when downloading the schema")]
    public string? AuthScheme { get; set; }
    
    [CommandOption(
        "headers",
        'x',
        Description = "Custom headers to use when downloading the schema. Example: --headers key1=value1 --headers key2=value2",
        Converter = typeof(HeaderConverter))]
    public KeyValuePair<string, string>[]? CustomHeaders { get; set; }
    
    [CommandOption(
        "timeout",
        Description = "Timeout in seconds for downloading the schema. Default is 30 seconds")]
    public int? Timeout { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var canContinue = await ReadConfig(console);
        if (!canContinue)
        {
            return;
        }
        
        if (Url is null)
        {
            await console.Output.WriteLineAsync("Url is required");
            return;
        }
        
        var cancellationToken = console.RegisterCancellationHandler();
        var (_, error) = await DownloadHelper.DownloadSchema(Url, Output, AccessToken, AuthScheme, CustomHeaders, Timeout, cancellationToken).Unwrap();
        if (error)
        {
            using var errorColor = console.WithForegroundColor(ConsoleColor.Red);
            await console.Error.WriteLineAsync(error.Message);
        }
    }
    
    private async Task<bool> ReadConfig(IConsole console)
    {
        if (Config is null)
        {
            return true;
        }

        var (config, error) = ZeroQLConfigReader.ReadConfig(Config).Unwrap();
        if (error)
        {
            using var redColor = console.WithForegroundColor(ConsoleColor.Red);
            await console.Error.WriteLineAsync("Error reading config file.");
            await console.Error.WriteLineAsync(error.Message);

            return false;
        }

        if (Url is null && !string.IsNullOrEmpty(config.Url))
        {
            Url = new Uri(config.Url);
        }

        if (string.IsNullOrEmpty(Output) && !string.IsNullOrEmpty(config.GraphQL))
        {
            Output = config.GraphQL;
        }

        if (!Timeout.HasValue)
        {
            Timeout = config.Timeout;
        }

        return true;
    }
}