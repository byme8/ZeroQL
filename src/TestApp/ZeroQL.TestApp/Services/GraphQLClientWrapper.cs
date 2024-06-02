using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GraphQL.TestServer;

namespace ZeroQL.TestApp.Services;

public class GraphQLClientWrapper
{
    private readonly TestServerClient client;

    public GraphQLClientWrapper(TestServerClient client)
    {
        this.client = client;
    }

    public async Task<TResult> QueryAsync<TResult>(
        [GraphQLLambda]
        Func<Query, TResult> query,
        [CallerArgumentExpression(nameof(query))]
        string queryKey = "")
        where TResult : class
    {
        var result = await client.Query(query, queryKey: queryKey);

        return result.Data!;
    }
}