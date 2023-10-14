using System.Text.Json;
using System.Text.Json.Serialization;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Newtonsoft.Json;
using ZeroQL.Config;
using ZeroQL.Core.Config;

namespace ZeroQL.CLI.Commands;

[Command("config init", Description = "Initialize a new configuration file")]
public class ConfigInitCommand : ICommand
{
    [CommandOption("output", 'o', Description = "Output file path, ./zeroql.json by default")]
    public string Output { get; set; } = "./config.zeroql.json";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        await console.Output.WriteLineAsync("Initializing configuration file...");
        await console.Output.WriteAsync("Input path to graphql schema file(default ./schema.graphql):");
        var graphql = await console.Input.ReadLineAsync();
        if (string.IsNullOrEmpty(graphql))
        {
            graphql = "./schema.graphql";
        }

        await console.Output.WriteAsync("Input namespace for GraphQL client(default ZeroQL.Client):");
        var @namespace = await console.Input.ReadLineAsync();
        if (string.IsNullOrEmpty(@namespace))
        {
            @namespace = "ZeroQL.Client";
        }

        await console.Output.WriteAsync("Input class name for GraphQL client(default ZeroQLClient):");
        var className = await console.Input.ReadLineAsync();
        if (string.IsNullOrEmpty(className))
        {
            className = "ZeroQLClient";
        }

        var config = new ZeroQLFileConfig()
        {
            Schema = "https://raw.githubusercontent.com/byme8/ZeroQL/main/schema.verified.json",
            GraphQL = graphql,
            Namespace = @namespace,
            ClientName = className
        };

        var json = JsonConvert.SerializeObject(config, ZeroQLSchema.GetJsonSerializerSettings());

        await File.WriteAllTextAsync(Output, json);
    }
}