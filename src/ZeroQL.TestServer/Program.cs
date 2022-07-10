using System.Net;
using ZeroQL.TestServer.Query;
using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer;

public class Program
{
    public const string TEST_SERVER_URL = "http://localhost:10000/graphql";

    private static readonly CancellationTokenSource CancellationTokenSource = new();

    public static async Task Main(string[] args)
    {
        await StartServer(args);
    }

    public static async Task StartServer(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(10_000));

        builder.Services.AddGraphQLServer()
            .AddQueryType<Query.Query>()
            .AddMutationType<Mutation>()
            .AddTypeExtension<UserGraphQLExtensions>()
            .AddTypeExtension<UserGraphQLMutations>()
            .AddTypeExtension<RoleGraphQLExtension>();

        var app = builder.Build();

        app.MapGraphQL();

        await app.RunAsync(CancellationTokenSource.Token);
    }

    public static async Task<bool> VerifyServiceIsRunning()
    {
        var httpClient = new HttpClient();
        for (var i = 0; i < 5; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(TEST_SERVER_URL + "?sdl");
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

    public static void StopServer()
    {
        CancellationTokenSource.Cancel();
    }
}