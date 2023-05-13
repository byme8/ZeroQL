using BenchmarkDotNet.Attributes;
using CliFx.Infrastructure;
using ZeroQL.CLI.Commands;

public class GenerationBenchmark
{
    public const string SchemaFile = "../../TestApp/ZeroQL.TestApp/schema.graphql";
    public const string OutputFile = "./bin/GraphQL.g.cs";


    [GlobalSetup]
    public void BeforeBenchmark()
    {

        if (!File.Exists(OutputFile))
        {
            return;
        }

        File.Delete(OutputFile);
    }

    [Benchmark]
    public async Task GenerateWithoutChecksumOptimization()
    {
        using var console = new FakeInMemoryConsole();
        var generateCommand = new GenerateCommand
        {
            Schema = SchemaFile,
            Output = OutputFile,
            Namespace = "GraphQL.Example",
            ClientName = "TestClient",
            Force = true,
        };

        await generateCommand.ExecuteAsync(console);
    }

    [Benchmark]
    public async Task GenerateWithChecksumOptimization()
    {
        using var console = new FakeInMemoryConsole();
        var generateCommand = new GenerateCommand
        {
            Schema = SchemaFile,
            Output = OutputFile,
            Namespace = "GraphQL.Example",
            ClientName = "TestClient"
        };

        await generateCommand.ExecuteAsync(console);
    }
}