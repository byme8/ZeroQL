using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LinqQL.Core;

public class GraphQLClient<TQuery> : IDisposable
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
        var result = await Execute<TQuery>(name, null, queryKey);
        if (result.Data is not null)
        {
            var formatted = query(variables, result.Data);
            return new GraphQLResult<TResult>
            {
                Query = result.Query,
                Data = formatted
            };
        }

        return new GraphQLResult<TResult>
        {
            Query = result.Query,
            Errors = result.Errors
        };
    }

    public async Task<GraphQLResult<TResult>> Query<TResult>(string name, Func<TQuery, TResult> query, [CallerArgumentExpression("query")] string queryKey = null!)
    {
        var result = await Execute<TQuery>(name, null, queryKey);
        if (result.Data is not null)
        {
            var formatted = query(result.Data);
            return new GraphQLResult<TResult>
            {
                Query = result.Query,
                Data = formatted
            };
        }

        return new GraphQLResult<TResult>
        {
            Query = result.Query,
            Errors = result.Errors
        };
    }

    public async Task<GraphQLResult<TResult>> Query<TVariables, TResult>(
        TVariables variables,
        Func<TVariables, TQuery, TResult> query,
        [CallerArgumentExpression("query")] string queryKey = null!)
    {
        var result = await Execute<TQuery>(null, variables, queryKey);
        if (result.Data is not null)
        {
            var formatted = query(variables, result.Data);
            return new GraphQLResult<TResult>
            {
                Query = result.Query,
                Data = formatted
            };
        }

        return new GraphQLResult<TResult>
        {
            Query = result.Query,
            Errors = result.Errors
        };
    }

    public async Task<GraphQLResult<TResult>> Query<TResult>(Func<TQuery, TResult> query, [CallerArgumentExpression("query")] string queryKey = null)
    {
        var result = await Execute<TQuery>(null, null, queryKey);
        if (result.Data is not null)
        {
            var formatted = query(result.Data);
            return new GraphQLResult<TResult>
            {
                Query = result.Query,
                Data = formatted
            };
        }

        return new GraphQLResult<TResult>
        {
            Query = result.Query,
            Errors = result.Errors
        };
    }

    public async Task<GraphQLResult<T>> Execute<T>(string? name, object? arguments, string queryKey)
    {
        if (!GraphQLQueryStore.Query.TryGetValue(queryKey, out var queryBody))
        {
            throw new InvalidOperationException("Query is not bootstrapped.");
        }

        var queryBuilder = new StringBuilder();
        queryBuilder.Append("query ");
        if (!string.IsNullOrEmpty(name))
        {
            queryBuilder.Append(name);
        }
        queryBuilder.Append(queryBody);

        var query = queryBuilder.ToString();
        var queryRequest = new GraphQLRequest
        {
            Variables = arguments,
            Query = query
        };

        var requestJson = JsonSerializer.Serialize(queryRequest, options);
        var response = await httpClient.PostAsync("", new StringContent(requestJson, Encoding.UTF8, "application/json"));
        var responseJson = await response.Content.ReadAsStringAsync();
        var qlResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(responseJson, options);
        if (qlResponse is null)
        {
            return new GraphQLResult<T>
            {
                Query = query,
                Errors = new[]
                {
                    new GraphQueryError { Message = "Failed to deserialize response: " + responseJson },
                }
            };
        }

        return new GraphQLResult<T>
        {
            Query = query,
            Data = qlResponse.Data,
            Errors = qlResponse.Errors
        };
    }
}