using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LinqQL.Core;

public class GraphQLClient<TQuery> : IDisposable
{
    private readonly HttpClient httpClient;
    private JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GraphQLClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public TResult Query<TArguments, TResult>(string name, TArguments arguments, Func<TArguments, TQuery, TResult> query)
    {
        return query(arguments, default);
    }

    public TResult Query<TResult>(string name, Func<TQuery, TResult> query)
    {
        return query(default);
    }

    public async Task<GraphQLResponse<TResult>> Query<TVariables, TResult>(TVariables variables, Func<TVariables, TQuery, TResult> query, [CallerArgumentExpression("query")] string queryKey = null)
    {
        var result = await Execute<TQuery>(variables, queryKey);
        var formatted = query(variables, result.Data);

        return new GraphQLResponse<TResult>
        {
            Data = formatted
        };
    }

    public async Task<GraphQLResponse<TResult>> Query<TResult>(Func<TQuery, TResult> query, [CallerArgumentExpression("query")] string queryKey = null)
    {
        var result = await Execute<TQuery>(null, queryKey);
        var formatted = query(result.Data);

        return new GraphQLResponse<TResult>
        {
            Data = formatted
        };
    }

    public async Task<GraphQLResponse<T>> Execute<T>(object? arguments, string queryKey)
    {
        if (!GraphQLQueryStore.Query.TryGetValue(queryKey, out var query))
        {
            throw new InvalidOperationException("Query is not bootstrapped.");
        }

        var queryRequest = new GraphQLRequest
        {
            Variables = arguments,
            Query = query
        };
        
        var json = JsonSerializer.Serialize(queryRequest, options);
        var response = await httpClient.PostAsync("", new StringContent(json, Encoding.UTF8, "application/json"));
        var stream = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GraphQLResponse<T>>(stream, options);

        return result;
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}

public class GraphQLRequest
{
    public object? Variables { get; set; }
    public string Query { get; set; }
}

public class GraphQLResponse<TData>
{
    public TData Data { get; set; }

    public GraphQueryError[] Errors { get; set; }
}

public class GraphQueryError
{
    public string Message { get; set; }
    public ErrorLocation[] Locations { get; set; }
}

public class ErrorLocation
{
    public int Line { get; set; }
    public int Column { get; set; }
}