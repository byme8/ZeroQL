using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using ZeroQL.Bootstrap;
using ZeroQL.Internal;

namespace ZeroQL.CLI.Commands;

[Command("generate", Description = "Generates C# classes from graphql file.")]
public class GenerateCommand : ICommand
{
    [CommandOption("schema", 's', Description = "The schema to generate the query. For example, './schema.graphql'", IsRequired = true)]
    public string Schema { get; set; }

    [CommandOption("namespace", 'n', Description = "The graphql client namespace", IsRequired = true)]
    public string Namespace { get; set; }

    [CommandOption("client-name", 'q', Description = "The graphql client name. Can be useful if you have multiple clients at the same time.", IsRequired = false)]
    public string? ClientName { get; set; }

    [CommandOption("output", 'o', Description = "The output file. For example, './Generated/GraphQL.g.cs'", IsRequired = true)]
    public string Output { get; set; }
    
    [CommandOption("force", 'f', Description = "Ignore checksum check and generate source code", IsRequired = false)]
    public bool Force { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!File.Exists(Schema))
        {
            await console.Error.WriteLineAsync("Schema file does not exist. Check that file exist.");
            return;
        }

        var fileName = Path.GetFileName(Output);
        if (string.IsNullOrEmpty(fileName))
        {
            await console.Error.WriteLineAsync("The output file name has wrong format. It should be path to file. For example, './Generated/GraphQL.g.cs'");
            return;
        }

        var options = new GraphQlGeneratorOptions(Namespace)
        {
            ClientName = ClientName
        };

        if (!Force && File.Exists(Output))
        {
            var checksumFile = ChecksumHelper.GenerateChecksumFromSchemaFile(Schema, options);
            var checksumSourceCode = ChecksumHelper.ExtractChecksumFromSourceCode(Output);

            if (checksumFile == checksumSourceCode)
            {
                await console.Output.WriteLineAsync("The source code is up-to-date with graphql schema. Skipping code generation.");
                return;
            }
        }

        var graphql = await File.ReadAllTextAsync(Schema);
        var csharpClient = GraphQLGenerator.ToCSharp(graphql, options);
        var outputFolder = Path.GetDirectoryName(Output)!;
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        await File.WriteAllTextAsync(Output, csharpClient);
    }
}