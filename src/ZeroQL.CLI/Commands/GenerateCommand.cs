using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using ZeroQL.Bootstrap;

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

        var graphql = await File.ReadAllTextAsync(Schema);
        var csharpClient = GraphQLGenerator.ToCSharp(graphql, Namespace, ClientName);
        var outputFolder = Path.GetDirectoryName(Output)!;
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        await File.WriteAllTextAsync(Output, csharpClient);
    }
}