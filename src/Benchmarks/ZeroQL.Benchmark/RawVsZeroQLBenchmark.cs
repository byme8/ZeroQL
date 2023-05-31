using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using GraphQL.TestServer;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.TestServerClient;

namespace ZeroQL.Benchmark;

[MemoryDiagnoser]
public class RawVsZeroQLBenchmark
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
    private readonly Upload upload;
    private readonly int id;

    public RawVsZeroQLBenchmark()
    {
        httpClient = new ImmortalHttpClientForStrawberryShake();
        httpClient.BaseAddress = new Uri("http://localhost:10000/graphql");

        zeroQLClient = new TestServerClient(httpClient);
        strawberryShake = StrawberryShakeTestServerClientCreator.Create(httpClient);
        upload = new Upload("image.png", new MemoryStream(new byte[42]));

        id = 1;
    }

    [Benchmark]
    public async Task<string> Raw()
    {
        var rawQuery = 
            $$"""
            {
                "variables": { "id": {{id}} }, 
                "query": "query GetUser($id: Int!){ user(id: $id) { id firstName lastName } }" 
            }
            """;
        var response = await httpClient.PostAsync("", new StringContent(rawQuery, Encoding.UTF8, "application/json"));
        var responseJson = await response.Content.ReadAsStreamAsync();
        var qlResponse = JsonSerializer.Deserialize<JsonObject>(responseJson, options);

        return qlResponse!["data"]!["user"]!["firstName"]!.GetValue<string>();
    }

    [Benchmark]
    public async Task<string> StrawberryShake()
    {
        var firstname = await strawberryShake.GetUser.ExecuteAsync(id);
        return firstname.Data!.User!.FirstName;
    }

    [Benchmark]
    public async Task<string> ZeroQLLambdaWithoutClosure()
    {
        var variables = new { Id = id };
        var firstname = await zeroQLClient.Query(
            variables, static (i, q)
                => q.User(i.Id, o => new { o.Id, o.FirstName, o.LastName }));

        return firstname.Data!.FirstName;
    }
    
    [Benchmark]
    public async Task<string> ZeroQLLambdaWithClosure()
    {
        var id  = this.id;
        var firstname = await zeroQLClient.Query( q
                => q.User(id, o => new { o.Id, o.FirstName, o.LastName }));

        return firstname.Data!.FirstName;
    }

    [Benchmark]
    public async Task<string> ZeroQLRequest()
    {
        var firstname = await zeroQLClient.Execute(new GetUserQuery(id));

        return firstname.Data!.FirstName;
    }

    [Benchmark]
    public async Task<int> ZeroQLLambdaUpload()
    {
        var variables = new { Id = 1, File = upload };
        var size = await zeroQLClient.Mutation(variables, static (i, m) => m.AddUserProfileImage(i.Id, i.File));

        return size.Data;
    }

    [Benchmark]
    public async Task<int> ZeroQLRequestUpload()
    {
        var size = await zeroQLClient.Execute(new AddAvatar(1, upload));

        return size.Data;
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