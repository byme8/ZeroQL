using System.Net;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.NodaTime;
using ZeroQL.TestServer.Query;
using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer;

public class Program
{
    public const string TEST_SERVER_URL_TEMPLATE = "http://localhost:{0}/graphql";

    public static async Task Main(string[] args)
    {
        await StartServer(new ServerContext
        {
            Arguments = args,
            Port = 10_000,
        });
    }

    public static IRequestExecutorBuilder AddBasicGraphQLServices(IServiceCollection services) =>
        services.AddGraphQLServer()
            .AddQueryType<Query.Query>()
            .AddMutationType<Mutation>()
            .AddType<UploadType>()
            .AddType<InstantType>()
            .AddTypeExtension<NodeTimeGraphQLExtensions>()
            .AddTypeExtension<UserGraphQLExtensions>()
            .AddTypeExtension<UserGraphQLMutations>()
            .AddTypeExtension<RoleGraphQLExtension>();

    public static async Task StartServer(ServerContext context)
    {
        var builder = WebApplication.CreateBuilder(context.Arguments);
        builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(context.Port));

        builder.Services.AddMemoryCache()
            .AddSha256DocumentHashProvider(HashFormat.Hex);

        var graphQLServer = AddBasicGraphQLServices(builder.Services);

        if (string.IsNullOrEmpty(context.QueriesPath))
        {
            graphQLServer
                .UseAutomaticPersistedQueryPipeline()
                .AddInMemoryQueryStorage();
        }
        else
        {
            graphQLServer
                .UsePersistedQueryPipeline()
                .AddFileSystemQueryStorage(context.QueriesPath);
        }

        var app = builder.Build();

        app.MapGraphQL();

        await app.RunAsync(context.CancellationTokenSource.Token);
    }

    public static async Task<bool> VerifyServiceIsRunning(ServerContext context)
    {
        var httpClient = new HttpClient();
        for (var i = 0; i < 5; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(string.Format(TEST_SERVER_URL_TEMPLATE, context.Port) + "?sdl");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch
            {
            }

            await Task.Delay(500);
        }

        return false;
    }

    public static void StopServer(ServerContext context)
    {
        context.CancellationTokenSource.Cancel();
    }

    public class ServerContext
    {
        public int Port { get; set; } = 10_000;

        public string[] Arguments { get; set; } = Array.Empty<string>();

        public CancellationTokenSource CancellationTokenSource { get; set; } = new();

        public string? QueriesPath { get; set; }
    }
}