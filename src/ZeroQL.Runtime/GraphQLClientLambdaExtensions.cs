using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ZeroQL;

// ReSharper disable once CheckNamespace
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public static class GraphQLClientLambdaExtensions
{
    public static async Task<GraphQLResult<TResult>> Query<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        TVariables variables,
        Func<TVariables, TQuery, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression(nameof(query))] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey, cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Query<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        TVariables variables,
        Func<TVariables, TQuery, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression(nameof(query))] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey, cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Query<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        Func<TQuery, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression(nameof(query))] string queryKey = null!)
    {
        var variables = GetVariables(query);
        return await client.Execute<Dictionary<string, object?>, TQuery, TResult>(
            variables,
            (_, q) => query(q),
            queryKey,
            cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Query<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        Func<TQuery, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression(nameof(query))] string queryKey = null!)
    {
        var variables = GetVariables(query);
        return await client.Execute<Dictionary<string, object?>, TQuery, TResult>(
            variables,
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
        [CallerArgumentExpression(nameof(query))] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey, cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TVariables, TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        TVariables variables,
        Func<TVariables, TMutation, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression(nameof(query))] string queryKey = null!)
    {
        return await client.Execute(variables, query!, queryKey, cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        string name,
        Func<TMutation, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression(nameof(query))] string queryKey = null!)
    {
        var variables = GetVariables(query);
        return await client.Execute<Dictionary<string, object?>, TMutation, TResult>(
            variables,
            (_, q) => query(q),
            queryKey,
            cancellationToken);
    }

    public static async Task<GraphQLResult<TResult>> Mutation<TQuery, TMutation, TResult>(
        this GraphQLClient<TQuery, TMutation> client,
        Func<TMutation, TResult> query,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression(nameof(query))] string queryKey = null!)
    {
        var variables = GetVariables(query);
        return await client.Execute<Dictionary<string, object?>, TMutation, TResult>(
            variables,
            (_, q) => query(q), queryKey,
            cancellationToken);
    }

    private static Dictionary<string, object?> GetVariables<TQuery, TResult>(Func<TQuery, TResult> query)
    {
        var fields = query.Target!.GetType().GetFields();
        var variables = fields
            .Where(o => !o.Name.StartsWith("<>"))
            .ToDictionary(o => o.Name, o => o.GetValue(query.Target));
        
        return variables;
    }
}