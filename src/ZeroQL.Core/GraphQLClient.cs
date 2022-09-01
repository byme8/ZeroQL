using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ZeroQL.Stores;

namespace ZeroQL;

public class GraphQLClient<TQuery, TMutation> : IDisposable
{
    private readonly HttpClient httpClient;

    public GraphQLClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<GraphQLResult<TResult>> Execute<TVariables, TOperationType, TResult>(
        string? operationName,
        TVariables? variables,
        Func<TVariables?, TOperationType?, TResult?> queryMapper,
        string queryKey)
    {
        if (!GraphQLQueryStore<TOperationType>.Query.TryGetValue(queryKey, out var queryRunner))
        {
            throw new InvalidOperationException("Query is not bootstrapped.");
        }

        var result = await queryRunner.Invoke(httpClient, operationName, variables);
        if (result.Errors?.Any() ?? false)
        {
            return new GraphQLResult<TResult>(result.Query, default, result.Errors);
        }

        return new GraphQLResult<TResult>(
            result.Query,
            queryMapper(variables, result.Data),
            result.Errors);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}