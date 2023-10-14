using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using ZeroQL.Config;
using ZeroQL.Core.Config;

namespace ZeroQL.CLI.Commands;

[Command("config output", Description = "Reads output from config file. Command as workaround for MSBuild inability to read json.")]
public class ConfigEchoOutputCommand : ICommand
{
    [CommandOption(
        "config",
        'c',
        Description =
            "The generation config file. For example, './zeroql.json'. Use `zeroql config init` to bootstrap.")]
    public string? Config { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var (config, error) = ZeroQLConfigReader.ReadConfig(Config).Unwrap();
        if (error)
        {
            using var redColor = console.WithForegroundColor(ConsoleColor.Red);
            await console.Error.WriteLineAsync("Error reading config file.");
            await console.Error.WriteLineAsync(error.Message);
            return;
        }
        
        await console.Output.WriteLineAsync(config.Output);
    }
}