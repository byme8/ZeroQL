using CliFx.Infrastructure;
using FluentAssertions;
using ZeroQL.Tests.Core;
using ZeroQL.CLI.Commands;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.CLI;

public class CLITests
{
    [Fact]
    public async Task Generate_CodeShouldCompile()
    {
        using var console = new FakeInMemoryConsole();
        
        var generateCommand = new GenerateCommand
        {
            Schema = "../../../../TestApp/ZeroQL.TestApp/schema.graphql",
            Namespace = "GraphQL.TestServer",
            ClientName = "TestServerClient",
            Output = "../../../../TestApp/ZeroQL.TestApp/Generated/GraphQL.g.cs",
            Force = true
        };

        await generateCommand.ExecuteAsync(console);

        console.ReadErrorString().Should().BeEmpty();
        await TestProject.Project.CompileToRealAssembly();
    }
    
    [Fact]
    public async Task Generate_ShouldNotGenerateCodeIfNotNeeded()
    {
        var outputFile = Path.GetTempFileName();
        
        using var console = new FakeInMemoryConsole();
        var generateCommand = new GenerateCommand
        {
            Schema = "../../../../TestApp/ZeroQL.TestApp/schema.graphql",
            Namespace = "GraphQL.TestServer",
            ClientName = "TestServerClient",
            Output = outputFile,
        };
        
        
        // should generate file the first time
        await generateCommand.ExecuteAsync(console);
        
        File.Exists(outputFile).Should().BeTrue();
        console.ReadErrorString().Should().BeEmpty();
        var lastWriteTime = File.GetLastWriteTime(outputFile);
        console.Clear();

        // nothing changed. should skip generation
        await generateCommand.ExecuteAsync(console);
        File.Exists(outputFile).Should().BeTrue();
        File.GetLastWriteTime(outputFile).Should().Be(lastWriteTime);
        console.ReadOutputString().Should().Contain("The source code is up-to-date with graphql schema. Skipping code generation.");
        console.Clear();
        
        // setting updated. should trigger new code generation
        generateCommand.ClientName = "UpdatedClientName";
        await generateCommand.ExecuteAsync(console);
        File.Exists(outputFile).Should().BeTrue();
        File.GetLastWriteTime(outputFile).Should().BeAfter(lastWriteTime);
    }

    [Fact]
    public async Task Extract()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var fileName = $"../../../../TestApp/ZeroQL.TestApp/bin/Debug/net6.0/{uniqueId}/TestApp.dll";
        var bytes = await TestProject.Project.CompileToRealAssemblyAsBytes();

        var path = Path.GetDirectoryName(fileName)!;
        Directory.CreateDirectory(path);
        await File.WriteAllBytesAsync(fileName, bytes);
        
        using var console = new FakeInMemoryConsole();
        var generateCommand = new ExtractQueriesCommand();
        generateCommand.AssemblyFile = fileName;
        generateCommand.ClientName = "GraphQL.TestServer.TestServerClient";
        generateCommand.Output = $"../../../../TestApp/ZeroQL.TestApp/bin/{uniqueId}/queries";

        await generateCommand.ExecuteAsync(console);

        console.ReadErrorString().Should().BeEmpty();
        Directory.EnumerateFiles(generateCommand.Output).Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task<ExtractQueriesCommand> ExtractMutationAndQuery()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var fileName = $"../../../../TestApp/ZeroQL.TestApp/bin/Debug/net6.0/{uniqueId}/TestApp.dll";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", "// place to replace", @"await qlClient.Mutation(static q => q.AddUser(""Jon"", ""Smith"", o => o.Id));");
        
        var bytes = await project.CompileToRealAssemblyAsBytes();
        
        var path = Path.GetDirectoryName(fileName)!;
        Directory.CreateDirectory(path);
        await File.WriteAllBytesAsync(fileName, bytes);
        
        using var console = new FakeInMemoryConsole();
        var command = new ExtractQueriesCommand();
        command.AssemblyFile = fileName;
        command.ClientName = "GraphQL.TestServer.TestServerClient";
        command.Output = $"../../../../TestApp/ZeroQL.TestApp/bin/queries/{uniqueId}/queries";

        await command.ExecuteAsync(console);

        console.ReadErrorString().Should().BeEmpty();
        Directory.EnumerateFiles(command.Output).Should().NotBeEmpty();

        return command;
    }
}