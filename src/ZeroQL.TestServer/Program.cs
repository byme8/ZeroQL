using System.Diagnostics;
using System.Net;
using HotChocolate.Language;
using HotChocolate.Types.NodaTime;
using ZeroQL.TestServer.Query;
using ZeroQL.TestServer.Query.Models;
using UuidType = ZeroQL.TestServer.Query.Models.UuidType;

namespace ZeroQL.TestServer;

public class Program
{
    public const string TestServerUrlTemplate = "http://localhost:{0}/graphql";

    public static async Task Main(string[] args)
    {
        await StartServer(new ServerContext
        {
            Arguments = args,
            Port = 10_000,
        });
    }

    public static async Task StartServer(ServerContext context)
    {
        var builder = WebApplication.CreateBuilder(context.Arguments);
        var app = CreateApp(context, builder);

        await app.RunAsync(context.CancellationTokenSource.Token);
    }

    public static WebApplication CreateApp(ServerContext context, WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(context.Port));

        builder.Services.AddMemoryCache()
            .AddSha256DocumentHashProvider(HashFormat.Hex);

        var graphQLServer = builder.Services.AddGraphQLServer()
            .AddTestServerTypes()
            .AddType<UploadType>()
            .AddType<InstantType>()
            .AddType<ZonedDateTimeType>()
            .AddType<IInterfaceThatNeverGetsUsed>()
            .AddType<Person>().BindRuntimeType<Uuid, UuidType>();

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

        app.Use(async (context, next) =>
        {
            var traceId = Guid.NewGuid().ToString();
            context.Response.Headers.TryAdd("trace-id", traceId);

            await next();
        });

        app.MapGraphQL();

        return app;
    }

    public static async Task<bool> VerifyServiceIsRunning(ServerContext context)
    {
        var httpClient = new HttpClient();
        for (var i = 0; i < 5; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(string.Format(TestServerUrlTemplate, context.Port) + "?sdl");
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