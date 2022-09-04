using System.Reflection;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using ZeroQL.Stores;

namespace ZeroQL.CLI.Commands;

[Command("queries extract", Description = "Generates C# classes from graphql file.")]
public class ExtractQueriesCommand : ICommand
{
    [CommandOption("assembly", 'a', Description = "The assembly that contains the graphql client. For example, './MyApp.dll'", IsRequired = true)]
    public string AssemblyFile { get; set; }

    [CommandOption("class", 'c', Description = "The graphql client class name. For example, 'TestGraphQLClient", IsRequired = true)]
    public string ClientName { get; set; }

    [CommandOption("output", 'o', Description = "The output folder. For example, './Queries'", IsRequired = true)]
    public string Output { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!File.Exists(AssemblyFile))
        {
            await console.Error.WriteLineAsync("The assembly file does not exist. Check that file exist.");
            return;
        }

        var assemblyAbsolutePath = Path.GetFullPath(AssemblyFile);
        var assembly = Assembly.LoadFile(assemblyAbsolutePath);
        var clientType = assembly.GetType(ClientName);
        if (clientType == null)
        {
            await console.Error.WriteLineAsync("The client class does not exist. Check that the class exist.");
            return;
        }

        var baseType = clientType.BaseType;
        if (baseType == null)
        {
            await console.Error.WriteLineAsync("The client class does not inherit from GraphQLClient. Check that the class inherit from GraphQLClient.");
            return;
        }

        var queryType = baseType.GenericTypeArguments.First();
        var mutationType = baseType.GenericTypeArguments.Last();

        var queries = GetPropertyFromStore(queryType);
        var mutations = GetPropertyFromStore(mutationType);

        var graphqlInfo = new List<QueryInfo>();
        if (queries != null)
        {
            graphqlInfo.AddRange(queries.Values);
        }

        if (mutations != null)
        {
            graphqlInfo.AddRange(mutations.Values);
        }

        var outputFolder = Output;

        if (File.Exists(Output))
        {
            Directory.Delete(outputFolder, true);
        }

        Directory.CreateDirectory(outputFolder);

        var tasks = graphqlInfo
            .Select(o => File.WriteAllTextAsync(Path.Combine(outputFolder, $"{o.Hash}.graphql"), o.Query))
            .ToArray();

        await Task.WhenAll(tasks);

        await console.Output.WriteLineAsync("Queries extracted: " + graphqlInfo.Count);
    }

    private static Dictionary<string, QueryInfo>? GetPropertyFromStore(Type queryType)
    {
        var queryStoreType = typeof(GraphQLQueryStore<>).MakeGenericType(queryType);
        var queries = (Dictionary<string, QueryInfo>?)queryStoreType
            .GetProperty("Query", BindingFlags.Static | BindingFlags.Public)?
            .GetValue(null);

        return queries;
    }
}