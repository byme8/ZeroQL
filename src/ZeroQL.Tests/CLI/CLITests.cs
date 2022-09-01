using CliFx.Infrastructure;
using ZeroQL.Tests.Core;
using Xunit;
using ZeroQL.CLI.Commands;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.CLI;

public class CLITests
{
    [Fact]
    public async Task Test()
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
}