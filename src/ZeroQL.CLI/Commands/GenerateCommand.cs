using System.Text.Json;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using ZeroQL.Bootstrap;
using ZeroQL.Internal;
using ZeroQL.Internal.Enums;
using ZeroQL.Json;

#pragma warning disable CS8618

namespace ZeroQL.CLI.Commands;

[Command("generate", Description = "Generates C# classes from graphql file.")]
public class GenerateCommand : ICommand
{
    [CommandOption(
        "config",
        'c',
        Description =
            "The generation config file. For example, './zeroql.json'. Use `zeroql config init` to bootstrap.",
        IsRequired = false)]
    public string? Config { get; set; }

    [CommandOption(
        "schema",
        's',
        Description = "The schema to generate the query. For example, './schema.graphql'",
        IsRequired = true)]
    public string Schema { get; set; }

    [CommandOption("namespace", 'n', Description = "The graphql client namespace", IsRequired = true)]
    public string Namespace { get; set; }

    [CommandOption(
        "client-name",
        'q',
        Description = "The graphql client name. Can be useful if you have multiple clients at the same time.",
        IsRequired = false)]
    public string? ClientName { get; set; }

    [CommandOption("output", 'o', Description = "The output file. For example, './Generated/GraphQL.g.cs'",
        IsRequired = true)]
    public string Output { get; set; }

    [CommandOption("access", 'a', Description = "The client visibility within the assembly", IsRequired = false)]
    public ClientVisibility? Visibility { get; set; }

    [CommandOption("force", 'f', Description = "Ignore checksum check and generate source code", IsRequired = false)]
    public bool Force { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var canContinue = await ReadConfig(console);
        if (!canContinue)
        {
            return;
        }

        if (!File.Exists(Schema))
        {
            await console.Error.WriteLineAsync("Schema file does not exist. Check that file exist.");
            return;
        }

        var fileName = Path.GetFileName(Output);
        if (string.IsNullOrEmpty(fileName))
        {
            await console.Error.WriteLineAsync(
                "The output file name has wrong format. It should be path to file. For example, './Generated/GraphQL.g.cs'");
            return;
        }

        var options = new GraphQlGeneratorOptions(Namespace, Visibility ?? ClientVisibility.Public)
        {
            ClientName = ClientName
        };

        if (!Force && File.Exists(Output))
        {
            var checksumFile = ChecksumHelper.GenerateChecksumFromSchemaFile(Schema, options);
            var checksumSourceCode = ChecksumHelper.ExtractChecksumFromSourceCode(Output);

            if (checksumFile == checksumSourceCode)
            {
                await console.Output.WriteLineAsync(
                    "The source code is up-to-date with graphql schema. Skipping code generation.");
                return;
            }
        }

        var graphql = await File.ReadAllTextAsync(Schema);
        var csharpClient = GraphQLGenerator.ToCSharp(graphql, options);
        var outputPath = Path.IsPathRooted(Output) ? Output : Path.GetFullPath(Output);
        var outputFolder = Path.GetDirectoryName(outputPath)!;
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        await File.WriteAllTextAsync(outputPath, csharpClient);
    }

    public async Task<bool> ReadConfig(IConsole console)
    {
        if (Config is null)
        {
            return true;
        }

        if (!File.Exists(Config))
        {
            await console.Error.WriteLineAsync($"Config file '{Config}' does not exist. Check that file exist.");
            return false;
        }

        var json = await File.ReadAllTextAsync(Config);
        var config = JsonSerializer.Deserialize<ZeroQLFileConfig>(json, ZeroQLJsonOptions.Options);
        if (config is null)
        {
            await console.Error.WriteLineAsync("Config file is not valid. Check that file is valid.");
            return false;
        }

        if (string.IsNullOrEmpty(Schema))
        {
            Schema = config.GraphQL;
        }

        if (string.IsNullOrEmpty(Namespace))
        {
            Namespace = config.Namespace;
        }

        if (string.IsNullOrEmpty(ClientName))
        {
            ClientName = config.ClientName;
        }

        if (!Visibility.HasValue)
        {
            Visibility = config.Visibility;
        }

        if (string.IsNullOrEmpty(Output))
        {
            Output = config.Output;
        }

        return true;
    }
}