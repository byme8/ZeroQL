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
        using var source = context.CancellationTokenSource;
        
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (
                    "http://localhost:10000/graphql",
                    "http://localhost:10001/graphql"
                ),
                (
                    "var qlClient = new TestServerClient(httpClient);",
                    "var qlClient = new TestServerClient(httpClient, new PersistedQueryPipeline());"
                ));

        var result = await project.Execute();
        context.CancellationTokenSource.Cancel();
        
        await Verify(result);
    }

    [Fact]
    public async Task FailsWhenPersistedQueryIsMissing()
    {
        var context = await RunServer(10_001);
        using var source = context.CancellationTokenSource;
        
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (
                    "http://localhost:10000/graphql",
                    "http://localhost:10001/graphql"
                ),
                (
                    "var qlClient = new TestServerClient(httpClient);",
                    "var qlClient = new TestServerClient(httpClient, new PersistedQueryPipeline(false));"
                ));

        var result = await project.Execute();
        context.CancellationTokenSource.Cancel();

        await Verify(result);
    }

    [Fact]
    public async Task CanSendPersistedQueryWhenServerHasIt()
    {
        var cli = new CliTests();
        var command = await cli.ExtractMutationAndQuery();

        var context = await RunServer(10_001, command.Output);
        using var source = context.CancellationTokenSource;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (
                    "http://localhost:10000/graphql",
                    "http://localhost:10001/graphql"
                ),
                (
                    "var qlClient = new TestServerClient(httpClient);",
                    "var qlClient = new TestServerClient(httpClient, new PersistedQueryPipeline(false));"
                ));

        var result = await project.Execute();
        context.CancellationTokenSource.Cancel();

        await Verify(result);
    }

    private static async Task<Program.ServerContext> RunServer(int port, string? queriesPath = null)
    {
        var context = new Program.ServerContext
        {
            Arguments = Array.Empty<string>(),
            QueriesPath = queriesPath,
            Port = port
        };

        if (await Program.VerifyServiceIsRunning(context))
        {
            throw new InvalidOperationException("Server is running");
        }

        var (app, _) = await Program.StartServer(context);
        context.CancellationTokenSource.Token.Register(async () =>
        {
            await app.DisposeAsync();
        });
        
        if (!await Program.VerifyServiceIsRunning(context))
        {
            throw new InvalidOperationException("Server failed to bootstrap");
        }

        return context;
    }
}