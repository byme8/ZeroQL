using BenchmarkDotNet.Attributes;
using GraphQL.TestServer;

namespace ZeroQL.Benchmark;

[MemoryDiagnoser]
public class OldVariablesVsNewVariablesBenchmark
{
    private readonly HttpClient httpClient;
    private readonly TestServerGraphQLClient zeroQLClient;

    public OldVariablesVsNewVariablesBenchmark()
    {
        httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:10000/graphql");

        zeroQLClient = new TestServerGraphQLClient(httpClient);
    }

    [Benchmark]
    public async Task<int> Old()
    {
        var variables = new { Id = UserKindPascal.SupperGood };
        var firstName = await zeroQLClient.Mutation(variables, static (i, q) => q.AddUserKindPascal(i.Id));
    
        return firstName.Data!;
    }

    [Benchmark]
    public async Task<int> New()
    {
        var id = UserKindPascal.SupperGood;
        var firstName = await zeroQLClient.Mutation(q => q.AddUserKindPascal(id));

        return firstName.Data!;
    }
}