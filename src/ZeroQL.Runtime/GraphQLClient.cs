using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZeroQL.Internal;
using ZeroQL.Pipelines;
using ZeroQL.Stores;

namespace ZeroQL;

public interface IGraphQLClient
{
    IHttpHandler HttpHandler { get; }

    IGraphQLQueryPipeline QueryPipeline { get; }

    Task<GraphQLResult<TResult>> Execute<TVariables, TOperationType, TResult>(
        TVariables? variables,
        Func<TVariables?, TOperationType, TResult?> queryMapper,
        string queryKey,
        CancellationToken cancellationToken = default);
}

public class ClientOperations
{
    public ClientOperations(
        Dictionary<string, QueryInfo>? queries,
        Dictionary<string, QueryInfo>? mutations)
    {
        Queries = queries;
        Mutations = mutations;
    }

    public Dictionary<string, QueryInfo>? Queries { get; }
    public Dictionary<string, QueryInfo>? Mutations { get; }

    public void Deconstruct(out Dictionary<string, QueryInfo>? queries, out Dictionary<string, QueryInfo>? mutations)
    {
        queries = Queries;
        mutations = Mutations;
    }
}

public class GraphQLClient<TQuery, TMutation> : IGraphQLClient, IDisposable
{
    public static ClientOperations GetBakedOperations()
    {
        return new ClientOperations(GraphQLQueryStore<TQuery>.Query, GraphQLQueryStore<TMutation>.Query);
    }

    public GraphQLClient(HttpClient httpClient, IGraphQLQueryPipeline? queryPipeline = null)
    {
        HttpHandler = new HttpHandler(httpClient);
        QueryPipeline = queryPipeline ?? new FullQueryPipeline();
    }

    public GraphQLClient(IHttpHandler httpClient, IGraphQLQueryPipeline? queryPipeline = null)
    {
        HttpHandler = httpClient;
        QueryPipeline = queryPipeline ?? new FullQueryPipeline();
    }

    public IHttpHandler HttpHandler { get; }

    public IGraphQLQueryPipeline QueryPipeline { get; }

    public async Task<GraphQLResult<TResult>> Execute<TVariables, TOperationType, TResult>(
        TVariables? variables,
        Func<TVariables?, TOperationType, TResult?> queryMapper,
        string queryKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedQueryKey = QueryKey.Normalize(queryKey);
        if (!GraphQLQueryStore<TOperationType>.Executor.TryGetValue(normalizedQueryKey, out var queryRunner))
        {
            throw new InvalidOperationException("Query is not bootstrapped.");
        }

        var context = new QueryExecuteContext
        {
            Client = this,
            QueryKey = normalizedQueryKey,
            Variables = variables,
            CancellationToken = cancellationToken
        };
        var result = await queryRunner.Invoke(context);

        if (result.Data is null)
        {
            return new GraphQLResult<TResult>(result.Query, default, result.Errors, result.Extensions);
        }
        
        var mappedData = queryMapper(variables, result.Data);
        return new GraphQLResult<TResult>(result.Query, mappedData, result.Errors, result.Extensions);
    }

    public void Dispose()
    {
        HttpHandler.Dispose();
    }
}