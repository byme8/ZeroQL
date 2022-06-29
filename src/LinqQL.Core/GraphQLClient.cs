using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace LinqQL.Core;

public class GraphQLClient<TQuery> : IDisposable
{
    private readonly HttpClient httpClient;

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
    
    public TResult Query<TArguments, TResult>(TArguments arguments, Func<TArguments, TQuery, TResult> query)
    {
        return query(arguments, default);
    }
    
    public TResult Query<TResult>(Func<TQuery, TResult> query)
    {
        return query(default);
    }
    
    public async Task<T> Execute<T>(string query)
    {
        var response = await httpClient.PostAsync("", new StringContent(query, Encoding.UTF8, "application/json"));
        var stream = await response.Content.ReadAsStreamAsync();
        var jsonObject = JsonNode.Parse(stream)["data"].AsObject();
        if (jsonObject.Count != 1)
        {
            throw new NotSupportedException($"Responses like {jsonObject.ToJsonString()} is not supported yet.");
        }

        var result = jsonObject.First().Value.Deserialize<T>(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return result;
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}