using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using ZeroQL.CLI.Converters;

namespace ZeroQL.CLI.Commands;

[Command("schema pull", Description = "Pulls the schema from a remote server")]
public class PullSchemaCommand : ICommand
{
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
        Description = "Timeout in seconds for the schema download operation. Use 0 for no timeout.",
        IsRequired = false)]
    public int? TimeoutInSeconds { get; set; }
    
    [CommandOption(
        "config",
        'c',
        Description =
            "The generation config file. For example, './zeroql.json'. Config will be used to read timeout if not specified directly.")]
    public string? Config { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (Url is null)
        {
            await console.Output.WriteLineAsync("Url is required");
            return;
        }
        
        var timeout = TimeoutInSeconds;
        if (timeout == null && !string.IsNullOrEmpty(Config))
        {
            try
            {
                var configResult = ZeroQL.Config.ZeroQLConfigReader.ReadConfig(Config);
                if (!configResult.Error)
                {
                    timeout = configResult.Value?.SchemaDownloadTimeoutInSeconds;
                }
            }
            catch (Exception ex)
            {
                await console.Error.WriteLineAsync($"Error reading config file: {ex.Message}");
            }
        }
        
        var cancellationToken = console.RegisterCancellationHandler();
        await DownloadHelper.DownloadSchema(Url, Output, AccessToken, AuthScheme, CustomHeaders, timeout, cancellationToken);
    }
}