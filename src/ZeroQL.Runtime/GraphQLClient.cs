using System;
using System.Collections.Generic;
using System.IO;
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

    IZeroQLSerializer Serialization { get; set; }

    Task<GraphQLResult<TResult>> Execute<TVariables, TOperationType, TResult>(
        TVariables? variables,
        Func<TVariables?, TOperationType, TResult?> queryMapper,
        string queryKey,
        CancellationToken cancellationToken = default);
}

public class ClientOperations(
    Dictionary<string, QueryInfo>? queries,
    Dictionary<string, QueryInfo>? mutations)
{
    public Dictionary<string, QueryInfo>? Queries { get; } = queries;
    public Dictionary<string, QueryInfo>? Mutations { get; } = mutations;
}

public enum PipelineType
{
    Full,
    PersistedManual,
    PersistedAuto,
}

public interface IZeroQLSerializer
{
    byte[] Serialize<T>(T value);

    Task Serialize<T>(Stream stream, T value, CancellationToken cancellationToken = default);

    T? Deserialize<T>(byte[] bytes);

    Task<T?> Deserialize<T>(Stream stream, CancellationToken cancellationToken = default);
}

public class GraphQLClient<TQuery, TMutation> : IGraphQLClient, IDisposable
{
    public static ClientOperations GetBakedOperations()
    {
        return new ClientOperations(GraphQLQueryStore<TQuery>.Query, GraphQLQueryStore<TMutation>.Query);
    }

    public GraphQLClient(
        HttpClient httpClient,
        IZeroQLSerializer serializer,
        PipelineType pipelineType = PipelineType.Full)
        : this(new HttpHandler(httpClient), serializer, pipelineType)
    {
    }

    public GraphQLClient(
        IHttpHandler httpClient,
        IZeroQLSerializer serializer,
        PipelineType pipelineType = PipelineType.Full)
    {
        HttpHandler = httpClient;
        Serialization = serializer;
        QueryPipeline = pipelineType switch
        {
            PipelineType.Full => new FullQueryPipeline(Serialization),
            PipelineType.PersistedManual => new PersistedQueryPipeline(Serialization, false),
            PipelineType.PersistedAuto => new PersistedQueryPipeline(Serialization),
            _ => throw new ArgumentOutOfRangeException(nameof(pipelineType))
        };
    }

    public IZeroQLSerializer Serialization { get; set; }

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
        if (result.Errors?.Any() ?? false)
        {
            return new GraphQLResult<TResult>(result.Query, default, result.Errors, result.Extensions);
        }

        return new GraphQLResult<TResult>(result.Query, queryMapper(variables, result.Data!), result.Errors,
            result.Extensions);
    }

    public void Dispose()
    {
        HttpHandler.Dispose();
    }
}