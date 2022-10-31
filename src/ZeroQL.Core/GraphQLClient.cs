using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ZeroQL.Pipelines;
using ZeroQL.Stores;

namespace ZeroQL;

public interface IGraphQLClient
{
    IGraphQLTransport Transport { get; }
    IGraphQLQueryPipeline QueryPipeline { get; }

    Task<GraphQLResult<TResult>> Execute<TVariables, TOperationType, TResult>(
        TVariables? variables,
        Func<TVariables?, TOperationType?, TResult?> queryMapper,
        string queryKey);
}

public record ClientOperations
(
    Dictionary<string, QueryInfo>? Queries,
    Dictionary<string, QueryInfo>? Mutations
);

public class GraphQLClient<TQuery, TMutation> : IGraphQLClient, IDisposable
{
    public static ClientOperations GetBakedOperations()
    {
        return new ClientOperations(GraphQLQueryStore<TQuery>.Query, GraphQLQueryStore<TMutation>.Query);
    }

    public GraphQLClient(HttpClient httpClient, IGraphQLQueryPipeline? queryPipeline = null)
        : this(new HttpTransport(httpClient), queryPipeline)
    {

    }

    public GraphQLClient(IGraphQLTransport transport, IGraphQLQueryPipeline? queryPipeline = null)
    {
        Transport = transport;
        QueryPipeline = queryPipeline ?? new FullQueryPipeline();
    }

    public IGraphQLTransport Transport { get; }

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
            return new GraphQLResult<TResult>(result.Query, default, result.Errors, result.Extensions);
        }

        return new GraphQLResult<TResult>(result.Query, queryMapper(variables, result.Data), result.Errors, result.Extensions);
    }

    public void Dispose()
    {
        if (Transport is IDisposable disposableTransport)
        {
            disposableTransport.Dispose();
        }
    }
}