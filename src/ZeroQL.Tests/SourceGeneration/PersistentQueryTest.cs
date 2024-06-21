using FluentAssertions;
using ZeroQL.Tests.CLI;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;
using ZeroQL.TestServer;

namespace ZeroQL.Tests.SourceGeneration;

public class PersistentQueryTest
{
    [Fact]
    public async Task CanPushPersistedQueryWhenServerDoesntHaveIt()
    {
        var context = await RunServer(10_001);

        var query = "8ed4d3e773b6f87d986cc128a716cfc85d030c5fe6a5b585ab0c0820ac5d9728:query { me { firstName } }";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (
                    "http://localhost:10000/graphql",
                    "http://localhost:10001/graphql"
                ),
                (
                    "var qlClient = new TestServerClient(httpClient);",
                    "var qlClient = new TestServerClient(httpClient, pipelineType: PipelineType.PersistedAuto);"
                ));

        await project.Validate(query);

        context.CancellationTokenSource.Cancel();
    }

    [Fact]
    public async Task FailsWhenPersistedQueryIsMissing()
    {
        var context = await RunServer(10_001);

        var query = "8ed4d3e773b6f87d986cc128a716cfc85d030c5fe6a5b585ab0c0820ac5d9728:query { me { firstName } }";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (
                    "http://localhost:10000/graphql",
                    "http://localhost:10001/graphql"
                ),
                (
                    "var qlClient = new TestServerClient(httpClient);",
                    "var qlClient = new TestServerClient(httpClient, pipelineType: PipelineType.PersistedManual);"
                ));

        var result = await project.Validate(query, false);
        result.Errors!
            .Select(o => o.Message)
            .Should()
            .Contain("PersistedQueryNotFound");

        context.CancellationTokenSource.Cancel();
    }

    [Fact]
    public async Task CanSendPersistedQueryWhenServerHasIt()
    {
        var command = await CliTests.ExtractMutationAndQuery();

        var context = await RunServer(10_001, command.Output);

        var query = "8ed4d3e773b6f87d986cc128a716cfc85d030c5fe6a5b585ab0c0820ac5d9728:query { me { firstName } }";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (
                    "http://localhost:10000/graphql",
                    "http://localhost:10001/graphql"
                ),
                (
                    "var qlClient = new TestServerClient(httpClient);",
                    "var qlClient = new TestServerClient(httpClient, pipelineType: PipelineType.PersistedManual);"
                ));

        await project.Validate(query);

        context.CancellationTokenSource.Cancel();
    }

    private static async Task<Program.ServerContext> RunServer(int port, string? queriesPath = null)
    {
        var context = new Program.ServerContext
        {
            Arguments = Array.Empty<string>(),
            QueriesPath = queriesPath,
            Port = port
        };

        _ = Program.StartServer(context);
        if (!await Program.VerifyServiceIsRunning(context))
        {
            throw new InvalidOperationException("Server failed to bootstrap");
        }

        return context;
    }
}