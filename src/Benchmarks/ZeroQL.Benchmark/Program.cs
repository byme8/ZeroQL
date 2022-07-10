using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GraphQL.TestServer;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.TestServerClient;
using ZeroQL.Core;

_ = ZeroQL.TestServer.Program.StartServer(args);
await ZeroQL.TestServer.Program.VerifyServiceIsRunning();

var benchmark = new RawVSZeroQL();
var raw = await benchmark.Raw();
var strawberry = await benchmark.StrawberryShake();
var zeroQL = await benchmark.ZeroQL();

BenchmarkRunner.Run<RawVSZeroQL>();

ZeroQL.TestServer.Program.StopServer();

[MemoryDiagnoser]
public class RawVSZeroQL
{
    private readonly JsonSerializerOptions options = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient httpClient;
    private readonly TestServerClient zeroQLClient;
    private readonly StrawberryShakeTestServerClient strawberryShake;

    public RawVSZeroQL()
    {
        httpClient = new ImmortalHttpClientForStrawberryShake();
        httpClient.BaseAddress = new Uri("http://localhost:10000/graphql");

        zeroQLClient = new TestServerClient(httpClient);
        strawberryShake = StrawberryShakeTestServerClientCreator.Create(httpClient);
    }

    [Benchmark]
    public async Task<string> Raw()
    {
        var rawQuery = "query { me { firstName }}";

        var queryRequest = new GraphQLRequest
        {
            Variables = null,
            Query = rawQuery
        };

        var requestJson = JsonSerializer.Serialize(queryRequest, options);
        var response = await httpClient.PostAsync("", new StringContent(requestJson, Encoding.UTF8, "application/json"));
        var responseJson = await response.Content.ReadAsStringAsync();
        var qlResponse = JsonSerializer.Deserialize<GraphQLResponse<Query>>(responseJson, options);

        return qlResponse.Data.__Me.FirstName;
    }

    [Benchmark]
    public async Task<string> StrawberryShake()
    {
        var firstname = await strawberryShake.Me.ExecuteAsync();
        return firstname.Data.Me.FirstName;
    }

    [Benchmark]
    public async Task<string> ZeroQL()
    {
        var firstname = await zeroQLClient.Query(static q => q.Me(o => o.FirstName));

        return firstname.Data;
    }
}

public static class StrawberryShakeTestServerClientCreator
{
    public static StrawberryShakeTestServerClient Create(HttpClient httpClient)
    {
        var services = new ServiceCollection();
        services.AddStrawberryShakeTestServerClient();
        services.AddSingleton<IHttpClientFactory>(new FakeFactory(httpClient));

        var provider = services.BuildServiceProvider();
        var client = provider.GetService<StrawberryShakeTestServerClient>()!;

        return client;
    }
    
    private class FakeFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public FakeFactory(HttpClient client)
        {
            this.client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return client;
        }
    }
}

public class ImmortalHttpClientForStrawberryShake : HttpClient
{
    protected override void Dispose(bool disposing)
    {
    }
}