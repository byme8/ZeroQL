using CliFx.Infrastructure;
using FluentAssertions;
using ZeroQL.Tests.Core;
using Xunit;
using ZeroQL.CLI.Commands;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.CLI;

public class CLITests
{
    [Fact]
    public async Task Generate()
    {
        using var console = new FakeInMemoryConsole();
        var generateCommand = new GenerateCommand();
        generateCommand.Schema = "../../../../TestApp/ZeroQL.TestApp/schema.graphql";
        generateCommand.Namespace = "GraphQL.TestServer";
        generateCommand.ClientName = "TestServerClient";
        generateCommand.Output = "../../../../TestApp/ZeroQL.TestApp/Generated/GraphQL.g.cs";

        await generateCommand.ExecuteAsync(console);

        console.ReadErrorString().Should().BeEmpty();
        await TestProject.Project.CompileToRealAssembly();
    }

    [Fact]
    public async Task Extract()
    {
        var uniqueId = Guid.NewGuid().ToString();
        var fileName = $"../../../../TestApp/ZeroQL.TestApp/bin/Debug/net6.0/{uniqueId}.dll";
        var bytes = await TestProject.Project.CompileToRealAssemblyAsBytes();
        
        await File.WriteAllBytesAsync(fileName, bytes);
        
        using var console = new FakeInMemoryConsole();
        var generateCommand = new ExtractQueriesCommand();
        generateCommand.AssemblyFile = fileName;
        generateCommand.ClientName = "GraphQL.TestServer.TestServerClient";
        generateCommand.Output = "../../../../TestApp/ZeroQL.TestApp/bin/queries/" + uniqueId;

        await generateCommand.ExecuteAsync(console);

        console.ReadErrorString().Should().BeEmpty();
        Directory.EnumerateFiles(generateCommand.Output).Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task<ExtractQueriesCommand> ExtractMutationAndQuery()
    {
        var uniqueId = Guid.NewGuid().ToString();
        var fileName = $"../../../../TestApp/ZeroQL.TestApp/bin/Debug/net6.0/{uniqueId}.dll";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", "// place to replace", @"await qlClient.Mutation(static q => q.AddUser(""Jon"", ""Smith"", o => o.Id));");
        
        var bytes = await project.CompileToRealAssemblyAsBytes();
        
        await File.WriteAllBytesAsync(fileName, bytes);
        
        using var console = new FakeInMemoryConsole();
        var command = new ExtractQueriesCommand();
        command.AssemblyFile = fileName;
        command.ClientName = "GraphQL.TestServer.TestServerClient";
        command.Output = "../../../../TestApp/ZeroQL.TestApp/bin/queries/" + uniqueId;

        await command.ExecuteAsync(console);

        console.ReadErrorString().Should().BeEmpty();
        Directory.EnumerateFiles(command.Output).Should().NotBeEmpty();

        return command;
    }
}