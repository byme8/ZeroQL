using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Newtonsoft.Json;
using ZeroQL.Bootstrap;
using ZeroQL.CLI.Converters;
using ZeroQL.Internal;
using ZeroQL.Internal.Enums;

namespace ZeroQL.CLI.Commands;

[Command("generate", Description = "Generates C# classes from graphql file.")]
public class GenerateCommand : ICommand
{
    [CommandOption(
        "config",
        'c',
        Description =
            "The generation config file. For example, './zeroql.json'. Use `zeroql config init` to bootstrap.")]
    public string? Config { get; set; }

    [CommandOption(
        "schema",
        's',
        Description = "The path to get the schema file. For example, './schema.graphql'")]
    public string Schema { get; set; }

    [CommandOption("namespace", 'n', Description = "The namespace for generated client")]
    public string Namespace { get; set; }

    [CommandOption(
        "client-name",
        'q',
        Description = "The client name for the generated client")]
    public string? ClientName { get; set; }

    [CommandOption("output", 'o', Description = "The output file. For example, './Generated/GraphQL.g.cs'")]
    public string Output { get; set; }

    [CommandOption("visibility", 'v', Description = "The visibility within the assembly for the generated client")]
    public ClientVisibility? Visibility { get; set; }

    [CommandOption(
        "scalars",
        Description = "Custom scalars to use when generating the client. Example: --scalars Point=Geometry.Point --scalars Rect=Geometry.Rect",
        Converter = typeof(HeaderConverter))]
    public KeyValuePair<string, string>[]? Scalars { get; set; }

    [CommandOption(
        "netstandard-compatibility",
        Description = "Enables netstandard compatibility during generation.")]
    public bool? NetstandardCompatibility { get; set; } = null;

    [CommandOption("force", 'f', Description = "Ignore checksum check and generate source code")]
    public bool Force { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var canContinue = await ReadConfig(console);
        if (!canContinue)
        {
            return;
        }

        var validationSuccessful = await Validate(console);
        if (!validationSuccessful)
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

        var scalars = Scalars?
            .ToDictionary(o => o.Key, o => o.Value);
        var options = new GraphQlGeneratorOptions(Namespace, Visibility ?? ClientVisibility.Public)
        {
            ClientName = ClientName,
            NetstandardCompatibility = NetstandardCompatibility,
            Scalars = scalars
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

    private async Task<bool> Validate(IConsole console)
    {
        if (string.IsNullOrEmpty(Schema))
        {
            await console.Error.WriteLineAsync("Schema file is not specified. Use --schema option.");
            return false;
        }

        if (string.IsNullOrEmpty(Namespace))
        {
            await console.Error.WriteLineAsync("Namespace is not specified. Use --namespace option.");
            return false;
        }

        if (string.IsNullOrEmpty(Output))
        {
            await console.Error.WriteLineAsync("Output file is not specified. Use --output option.");
            return false;
        }

        return true;
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

        var schema = ZeroQLSchema.GetJsonSchema();
        var json = await File.ReadAllTextAsync(Config);
        var errors = schema.Validate(json);

        if (errors.Count > 0)
        {
            await console.Error.WriteLineAsync("Config file is not valid.");
            await console.Error.WriteLineAsync("Errors:");
            foreach (var error in errors)
            {
                var humanReadableError = ZeroQLSchema.GetHumanReadableErrorMessage(error.Kind);
                await console.Error.WriteLineAsync($"    {Config} [{error.LineNumber}:{error.LinePosition}]: {humanReadableError} at {error.Path}");
            }
            return false;
        }
        
        var config = JsonConvert.DeserializeObject<ZeroQLFileConfig>(json);
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

        if (Scalars is null)
        {
            Scalars = config.Scalars?.ToArray();
        }

        if (!NetstandardCompatibility.HasValue)
        {
            NetstandardCompatibility = config.NetstandardCompatibility;
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