using System.Reflection;
using System.Runtime.Loader;
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

        var absolutePathAssemblyFile = Path.GetFullPath(AssemblyFile);
        var context = AssemblyLoadContext.Default;
        LoadAssemblyWithDependencies(context, absolutePathAssemblyFile);

        var clientType = context.Assemblies
            .Select(o => o.GetType(ClientName))
            .FirstOrDefault(o => o is not null);

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

        ForceModuleInitializersToRun(context);

        var method = baseType.GetMethod("GetBakedOperations", BindingFlags.Static | BindingFlags.Public);
        var clientOperations = (ClientOperations)method!.Invoke(null, null)!;

        var queries = clientOperations.Queries;
        var mutations = clientOperations.Mutations;

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

    private static void ForceModuleInitializersToRun(AssemblyLoadContext context)
    {
        foreach (var assembly in context.Assemblies)
        {
            var initializers = assembly
                .ExportedTypes
                .Where(o => o.FullName?.Contains("ZeroQLModuleInitializer") ?? false)
                .ToArray();

            foreach (var initializer in initializers)
            {
                initializer
                    .GetMethod("Init", BindingFlags.Static | BindingFlags.Public)!
                    .Invoke(null, null);
            }
        }
    }

    private static Assembly? LoadAssemblyWithDependencies(AssemblyLoadContext context, string assemblyPath)
    {
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        var loaded = new HashSet<string>();
        LoadReferences(context, assembly, loaded);

        return assembly;

        static void LoadReferences(AssemblyLoadContext context, Assembly assembly, HashSet<string> loaded)
        {
            var assemblyNames = assembly
                .GetReferencedAssemblies();

            foreach (var assemblyName in assemblyNames.Where(o => !loaded.Contains(o.FullName)))
            {
                try
                {
                    var childAssembly = context.LoadFromAssemblyName(assemblyName);
                    loaded.Add(assemblyName.FullName);
                    LoadReferences(context, childAssembly, loaded);
                }
                catch
                {
                    try
                    {
                        var directory = Path.GetDirectoryName(assembly.Location);
                        var childAssembly = context.LoadFromAssemblyPath(Path.Combine(directory!, assemblyName.Name + ".dll"));
                        loaded.Add(assemblyName.FullName);
                        LoadReferences(context, childAssembly, loaded);
                    }
                    catch
                    {
                        // ignored
                    }

                    // ignored
                }
            }
        }
    }
}