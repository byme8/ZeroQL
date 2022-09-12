using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ZeroQL.Pipelines;
using ZeroQL.Stores;

namespace ZeroQL;

public interface IGraphQLClient
{
    HttpClient HttpClient { get; }

    IGraphQLQueryPipeline QueryPipeline { get; }

    Task<GraphQLResult<TResult>> Execute<TVariables, TOperationType, TResult>(
        TVariables? variables,
        Func<TVariables?, TOperationType?, TResult?> queryMapper,
        string queryKey);
}

public class GraphQLClient<TQuery, TMutation> : IGraphQLClient, IDisposable
{

    public GraphQLClient(HttpClient httpClient, IGraphQLQueryPipeline? queryPipeline = null)
    {
        HttpClient = httpClient;
        QueryPipeline = queryPipeline ?? new FullQueryPipeline();
    }

    public HttpClient HttpClient { get; }

    public IGraphQLQueryPipeline QueryPipeline { get; }

    public async Task<GraphQLResult<TResult>> Execute<TVariables, TOperationType, TResult>(
        TVariables? variables,
        Func<TVariables?, TOperationType?, TResult?> queryMapper,
        string queryKey)
    {
        if (!GraphQLQueryStore<TOperationType>.Executor.TryGetValue(queryKey, out var queryRunner))
        {
            throw new InvalidOperationException("Query is not bootstrapped.");
        }

        var result = await queryRunner.Invoke(this, queryKey, variables);
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
        HttpClient.Dispose();
    }
}