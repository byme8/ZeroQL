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
    HttpHandler HttpClient { get; }

    IGraphQLQueryPipeline QueryPipeline { get; }

    Task<GraphQLResult<TResult>> Execute<TVariables, TOperationType, TResult>(
        TVariables? variables,
        Func<TVariables?, TOperationType, TResult?> queryMapper,
        string queryKey,
        CancellationToken cancellationToken = default);
}

public interface HttpHandler : IDisposable
{
    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

public class HttpClientHandler : HttpHandler
{
    private HttpClient _client;

    public HttpClientHandler(HttpClient client)
    {
        _client = client;
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _client.SendAsync(request, cancellationToken);
    }
}

public class ClientOperations
{
    public ClientOperations(
        Dictionary<string, QueryInfo>? queries,
        Dictionary<string, QueryInfo>? mutations)
    {
        this.Queries = queries;
        this.Mutations = mutations;
    }

    public Dictionary<string, QueryInfo>? Queries { get; }
    public Dictionary<string, QueryInfo>? Mutations { get; }

    public void Deconstruct(out Dictionary<string, QueryInfo>? queries, out Dictionary<string, QueryInfo>? mutations)
    {
        queries = this.Queries;
        mutations = this.Mutations;
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
        HttpClient = new HttpClientHandler(httpClient);
        QueryPipeline = queryPipeline ?? new FullQueryPipeline();
    }
    
    public GraphQLClient(HttpHandler httpClient, IGraphQLQueryPipeline? queryPipeline = null)
    {
        HttpClient = httpClient;
        QueryPipeline = queryPipeline ?? new FullQueryPipeline();
    }

    public HttpHandler HttpClient { get; }

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
        if (result.Errors?.Any() ?? false)
        {
            return new GraphQLResult<TResult>(result.Query, default, result.Errors, result.Extensions);
        }

        return new GraphQLResult<TResult>(result.Query, queryMapper(variables, result.Data!), result.Errors,
            result.Extensions);
    }

    public void Dispose()
    {
        HttpClient.Dispose();
    }
}