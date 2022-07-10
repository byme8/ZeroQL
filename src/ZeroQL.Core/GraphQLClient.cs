using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ZeroQL.Core;

public class Unit
{
}

public enum OperationKind
{
    Query,
    Mutation
}

public class GraphQLClient<TQuery, TMutation> : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions options = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GraphQLClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    public async Task<GraphQLResult<TResult>> Query<TVariables, TResult>(
        string name,
        TVariables variables,
        Func<TVariables, TQuery, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute(OperationKind.Query, name, variables, query!, queryKey);
    }

    public async Task<GraphQLResult<TResult>> Query<TVariables, TResult>(
        TVariables variables,
        Func<TVariables, TQuery, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute(OperationKind.Query, null, variables, query!, queryKey);
    }

    public async Task<GraphQLResult<TResult>> Query<TResult>(
        string name,
        Func<TQuery?, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute<Unit, TQuery, TResult>(OperationKind.Query, name, null, (i, q) => query(q), queryKey);
    }

    public async Task<GraphQLResult<TResult>> Query<TResult>(
        Func<TQuery, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute<Unit, TQuery, TResult>(OperationKind.Query, null, null, (i, q) => query(q), queryKey);
    }

    public async Task<GraphQLResult<TResult>> Mutation<TVariables, TResult>(
        string name,
        TVariables variables,
        Func<TVariables, TMutation, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute(OperationKind.Mutation, name, variables, query!, queryKey);
    }

    public async Task<GraphQLResult<TResult>> Mutation<TVariables, TResult>(
        TVariables variables,
        Func<TVariables, TMutation, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute(OperationKind.Mutation, null, variables, query!, queryKey);
    }

    public async Task<GraphQLResult<TResult>> Mutation<TResult>(
        string name,
        Func<TMutation?, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute<Unit, TMutation, TResult>(OperationKind.Mutation, name, null, (i, q) => query(q), queryKey);
    }

    public async Task<GraphQLResult<TResult>> Mutation<TResult>(
        Func<TMutation, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute<Unit, TMutation, TResult>(OperationKind.Mutation, null, null, (i, q) => query(q), queryKey);
    }

    public async Task<GraphQLResult<TResult>> Execute<TVariables, TOperationQuery, TResult>(OperationKind operationKind,
        string? operationName,
        TVariables? arguments,
        Func<TVariables?, TOperationQuery?, TResult> queryMapper,
        string queryKey)
    {
        if (!GraphQLQueryStore.Query.TryGetValue(queryKey, out var queryBody))
        {
            throw new InvalidOperationException("Query is not bootstrapped.");
        }

        var queryBuilder = new StringBuilder();
        queryBuilder.Append(operationKind == OperationKind.Query ? "query " : "mutation ");
        if (!string.IsNullOrEmpty(operationName))
        {
            queryBuilder.Append(operationName);
        }
        queryBuilder.Append(queryBody);

        var query = queryBuilder.ToString();
        var queryRequest = new GraphQLRequest
        {
            Variables = arguments,
            Query = query
        };

        var qlResponse = await SendRequest<TOperationQuery>(queryRequest);

        if (qlResponse.Errors is { Length: > 1 })
        {
            return new GraphQLResult<TResult>
            {
                Query = query,
                Errors = qlResponse.Errors
            };
        }

        var formatted = queryMapper(arguments, qlResponse.Data);
        return new GraphQLResult<TResult>
        {
            Query = query,
            Data = formatted
        };
    }

    private async Task<GraphQLResponse<TOperationQuery>> SendRequest<TOperationQuery>(GraphQLRequest queryRequest)
    {
        var requestJson = JsonSerializer.Serialize(queryRequest, options);
        var response = await httpClient.PostAsync("", new StringContent(requestJson, Encoding.UTF8, "application/json"));
        var responseJson = await response.Content.ReadAsStringAsync();
        var qlResponse = JsonSerializer.Deserialize<GraphQLResponse<TOperationQuery>>(responseJson, options);

        if (qlResponse is null)
        {
            return new GraphQLResponse<TOperationQuery>
            {
                Errors = new[]
                {
                    new GraphQueryError { Message = "Failed to deserialize response: " + responseJson }
                }
            };
        }

        return qlResponse;
    }
}