using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ZeroQL.Core.Extensions;
using ZeroQL.Core.Stores;

namespace ZeroQL.Core;

public class Unit
{
}

public enum OperationKind
{
    Query,
    Mutation
}

public class GraphQLEnumNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return name.ToUpperCase();
    }
}

public static class ZeroQLJsonOptions
{
    public static JsonSerializerOptions Options = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(new GraphQLEnumNamingPolicy())
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

public class GraphQLClient<TQuery, TMutation> : IDisposable
{
    private readonly HttpClient httpClient;

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
        return await Execute(name, variables, query!, queryKey);
    }

    public async Task<GraphQLResult<TResult>> Query<TVariables, TResult>(
        TVariables variables,
        Func<TVariables, TQuery, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute(null, variables, query!, queryKey);
    }

    public async Task<GraphQLResult<TResult>> Query<TResult>(
        string name,
        Func<TQuery?, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute<Unit, TQuery, TResult>(name, null, (i, q) => query(q), queryKey);
    }

    public async Task<GraphQLResult<TResult>> Query<TResult>(
        Func<TQuery, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute<Unit, TQuery, TResult>(null, null, (i, q) => query(q), queryKey);
    }

    public async Task<GraphQLResult<TResult>> Mutation<TVariables, TResult>(
        string name,
        TVariables variables,
        Func<TVariables, TMutation, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute(name, variables, query!, queryKey);
    }

    public async Task<GraphQLResult<TResult>> Mutation<TVariables, TResult>(
        TVariables variables,
        Func<TVariables, TMutation, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute(null, variables, query!, queryKey);
    }

    public async Task<GraphQLResult<TResult>> Mutation<TResult>(
        string name,
        Func<TMutation?, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute<Unit, TMutation, TResult>(name, null, (i, q) => query(q), queryKey);
    }

    public async Task<GraphQLResult<TResult>> Mutation<TResult>(
        Func<TMutation, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        return await Execute<Unit, TMutation, TResult>(null, null, (i, q) => query(q), queryKey);
    }

    public async Task<GraphQLResult<TResult>> Execute<TVariables, TOperationQuery, TResult>(
        string? operationName,
        TVariables? variables,
        Func<TVariables?, TOperationQuery?, TResult> queryMapper,
        string queryKey)
    {
        if (!GraphQLQueryStore<TVariables, TOperationQuery>.Query.TryGetValue(queryKey, out var queryRunner))
        {
            throw new InvalidOperationException("Query is not bootstrapped.");
        }

        var result = await queryRunner.Invoke(httpClient, operationName, variables);
        var typedResult = result;

        return new GraphQLResult<TResult>(
            typedResult.Query,
            queryMapper(variables, typedResult.Data),
            typedResult.Errors);
    }

    // public async Task<GraphQLResult<TResult>> ExecuteQL<TVariables, TOperationQuery, TResult>(TVariables? variables, Func<TVariables?, TOperationQuery?, TResult> mapper, string query)
    // {
    //     var queryRequest = new GraphQLRequest
    //     {
    //         Variables = variables,
    //         Query = query
    //     };
    //
    //     var qlResponse = await SendRequest<TOperationQuery>(queryRequest);
    //
    //     if (qlResponse.Errors is { Length: > 0 })
    //     {
    //         return new GraphQLResult<TResult>
    //         {
    //             Query = query,
    //             Errors = qlResponse.Errors
    //         };
    //     }
    //
    //     var formatted = mapper(variables, qlResponse.Data);
    //     return new GraphQLResult<TResult>
    //     {
    //         Query = query,
    //         Data = formatted
    //     };
    // }

    private async Task<GraphQLResponse<TOperationQuery>> SendRequest<TOperationQuery>(GraphQLRequest queryRequest)
    {
        var requestJson = JsonSerializer.Serialize(queryRequest, ZeroQLJsonOptions.Options);
        var response = await httpClient.PostAsync("", new StringContent(requestJson, Encoding.UTF8, "application/json"));
        var responseJson = await response.Content.ReadAsStringAsync();
        var qlResponse = JsonSerializer.Deserialize<GraphQLResponse<TOperationQuery>>(responseJson, ZeroQLJsonOptions.Options);

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