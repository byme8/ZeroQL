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

        await TestProject.Project.CompileToRealAssembly();
    }

    [Fact]
    public async Task Extract()
    {
        var fileName = "../../../../TestApp/ZeroQL.TestApp/bin/Debug/net6.0/" +  Guid.NewGuid();
        var bytes = await TestProject.Project.CompileToRealAssemblyAsBytes();
        
        await File.WriteAllBytesAsync(fileName, bytes);
        
        using var console = new FakeInMemoryConsole();
        var generateCommand = new ExtractQueriesCommand();
        generateCommand.AssemblyFile = fileName;
        generateCommand.ClientName = "GraphQL.TestServer.TestServerClient";
        generateCommand.Output = "../../../../TestApp/ZeroQL.TestApp/bin/queries";

        await generateCommand.ExecuteAsync(console);

        Directory.EnumerateFiles(generateCommand.Output).Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task ExtractMutationAndQuery()
    {
        var uniqueId = Guid.NewGuid().ToString();
        var fileName = "../../../../TestApp/ZeroQL.TestApp/bin/Debug/net6.0/" + uniqueId;
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", "// place to replace", @"await qlClient.Mutation(static q => q.AddUser(""Jon"", ""Smith"", o => o.Id));");
        
        var bytes = await project.CompileToRealAssemblyAsBytes();
        
        await File.WriteAllBytesAsync(fileName, bytes);
        
        using var console = new FakeInMemoryConsole();
        var generateCommand = new ExtractQueriesCommand();
        generateCommand.AssemblyFile = fileName;
        generateCommand.ClientName = "GraphQL.TestServer.TestServerClient";
        generateCommand.Output = "../../../../TestApp/ZeroQL.TestApp/bin/queries/" + uniqueId;

        await generateCommand.ExecuteAsync(console);

        Directory.EnumerateFiles(generateCommand.Output).Should().NotBeEmpty();;
    }
}