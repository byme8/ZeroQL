using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using LinqQL.Core.Bootstrap;

namespace LinqQL.CLI.Commands;

[Command]
public class GenerateCommand : ICommand
{
    [CommandOption("schema", 's', Description = "The schema to generate the query for", IsRequired = true)]
    public string Schema { get; set; }

    [CommandOption("namespace", 'n', Description = "The query namespace", IsRequired = true)]
    public string Namespace { get; set; }

    [CommandOption("query-name", 'q', Description = "The query name", IsRequired = true)]
    public string? QueryName { get; set; }

    [CommandOption("output", 'o', Description = "The output file", IsRequired = true)]
    public string Output { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!File.Exists(Schema))
        {
            await console.Error.WriteLineAsync("Schema file does not exist");
            return;
        }


        var graphql = await File.ReadAllTextAsync(Schema);
        var csharpClient = GraphQLGenerator.ToCSharp(graphql, Namespace);
        var outputFolder = Path.GetDirectoryName(Output); 
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        await File.WriteAllTextAsync(Output, csharpClient);
    }
}