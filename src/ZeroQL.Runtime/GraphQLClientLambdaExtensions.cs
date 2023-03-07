using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ZeroQL;

// ReSharper disable once CheckNamespace
public static class GraphQLClientRequestExtensions
{
    public static async Task<GraphQLResult<TResult>> Execute<TQuery, TResult>(
        this IGraphQLClient client, GraphQL<TQuery, TResult> request, CancellationToken cancellationToken = default)
    {
        return await client.Execute<GraphQL<TQuery, TResult>, TQuery, TResult>(
            request,
            (_, q) => request.Execute(q),
            request.GetType().Name,
            cancellationToken);
    }
}

[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public static class GraphQLClientLambdaExtensions
{
    public static async Task<GraphQLResult<TResult>> Query<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        TVariables variables,
        Func<TVariables, TQuery, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey, cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Query<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        TVariables variables,
        Func<TVariables, TQuery, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey, cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Query<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        Func<TQuery?, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute<Unit, TQuery, TResult>(
            null,
            (_, q) => query(q),
            queryKey,
            cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Query<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        Func<TQuery, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute<Unit, TQuery, TResult>(
            null,
            (_, q) => query(q),
            queryKey,
            cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        TVariables variables,
        Func<TVariables, TMutation, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey, cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        TVariables variables,
        Func<TVariables, TMutation, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey, cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        Func<TMutation?, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute<Unit, TMutation, TResult>(
            null,
            (_, q) => query(q),
            queryKey,
            cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        Func<TMutation, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await client.Execute<Unit, TMutation, TResult>(
            null,
            (_, q) => query(q), queryKey,
            cancellationToken);
    }
}