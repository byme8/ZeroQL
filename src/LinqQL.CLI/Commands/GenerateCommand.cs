using System.Xml;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using LinqQL.Core.Bootstrap;
namespace LinqQL.CLI.Commands
{
    [Command]
    public class GenerateCommand : ICommand
    {
        public string? Project { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (!File.Exists(Project))
            {
                await console.Error.WriteLineAsync("Project file not found");
                return;
            }

            var project = new XmlDocument();
            project.Load(Project);

            var tagsWithSchema = project.GetElementsByTagName("GraphQLSchema");
            var entries = new List<GraphQLSchemaEntry>();
            for (int i = 0; i < tagsWithSchema.Count; i++)
            {
                var schemaNode = tagsWithSchema[i];
                if (schemaNode is null)
                {
                    break;
                }
                var schemaFile = schemaNode.Attributes?["Include"];
                var clientNamespace = schemaNode.Attributes?["Namespace"];
                var queryName = schemaNode.Attributes?["QueryName"];

                if (schemaFile is null)
                {
                    await console.Error.WriteLineAsync("Schema file is required.");
                    break;
                }

                if (clientNamespace is null)
                {
                    await console.Error.WriteLineAsync("Namespace is required.");
                    break;
                }

                if (queryName is null)
                {
                    await console.Error.WriteLineAsync("Query name is required.");
                    break;
                }

                entries.Add(new GraphQLSchemaEntry(schemaFile.Value, clientNamespace.Value, queryName.Value));
            }

            var projectFolder = Path.GetDirectoryName(Project);
            if (projectFolder is null)
            {
                await console.Error.WriteLineAsync("Project folder not found");
                return;
            }

            foreach (var entry in entries)
            {
                var graphql = await File.ReadAllTextAsync(Path.Combine(projectFolder, entry.Schema));
                var csharpClient = GraphQLGenerator.ToCSharp(graphql, entry.Namespace);
                var outputFolder = Path.Combine(projectFolder, entry.Namespace.Replace(".", "/"));
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                await File.WriteAllTextAsync(Path.Combine(outputFolder, entry.QueryName + ".g.cs"), csharpClient);
            }
        }
    }
}