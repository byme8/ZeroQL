using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ZeroQL.Internal;
using ZeroQL.Stores;

namespace ZeroQL;

public interface IGraphQLQueryStrategy
{
    GraphQLRequest CreateRequest<T>(string queryKey, string? name, object? variables);

    Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(HttpClient httpClient, HttpContent content);
}

public class FullQueryStrategy : IGraphQLQueryStrategy
{
    public GraphQLRequest CreateRequest<TQuery>(string queryKey, string? name, object? variables)
    {
        var queryInfo = GraphQLQueryStore<TQuery>.Query[queryKey];
        var stringBuilder = new System.Text.StringBuilder();
        stringBuilder.Append(queryInfo.OperationType);
        stringBuilder.Append(' ');
        if (!string.IsNullOrEmpty(name))
        {
            stringBuilder.Append(name);
        }
        stringBuilder.Append(queryInfo.QueryBody);

        var query = stringBuilder.ToString();
        return new GraphQLRequest
        {
            Query = query,
            Variables = variables
        };
    }

    public async Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(HttpClient httpClient, HttpContent content)
    {
        var response = await httpClient.PostAsync("", content);
        var responseJson = await response.Content.ReadAsStreamAsync();
        var qlResponse = await JsonSerializer.DeserializeAsync<GraphQLResponse<TQuery>>(responseJson, ZeroQLJsonOptions.Options);

        return qlResponse!;
    }
}