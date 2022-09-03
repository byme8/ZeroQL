using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ZeroQL.Internal;
using ZeroQL.Stores;

namespace ZeroQL;

public interface IGraphQLQueryStrategy
{
    Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(HttpClient httpClient, string queryKey, object? variables, Func<GraphQLRequest, HttpContent> contentCreator);
}

public class FullQueryStrategy : IGraphQLQueryStrategy
{
    public async Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(HttpClient httpClient, string queryKey, object? variables, Func<GraphQLRequest, HttpContent> contentCreator)
    {
        var queryInfo = GraphQLQueryStore<TQuery>.Query[queryKey];
        var query = queryInfo.Query;
        var qlRequest = new GraphQLRequest
        {
            Query = query,
            Variables = variables
        };

        var content = contentCreator(qlRequest);
        var response = await httpClient.PostAsync("", content);
        var responseJson = await response.Content.ReadAsStreamAsync();
        var qlResponse = await JsonSerializer.DeserializeAsync<GraphQLResponse<TQuery>>(responseJson, ZeroQLJsonOptions.Options);

        if (qlResponse is not null)
        {
            qlResponse.Query = query;
        }

        return qlResponse!;
    }
}

public class PersistentQueryStrategy : IGraphQLQueryStrategy
{
    public async Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(HttpClient httpClient, string queryKey, object? variables, Func<GraphQLRequest, HttpContent> contentCreator)
    {
        var queryInfo = GraphQLQueryStore<TQuery>.Query[queryKey];
        var qlRequest = new GraphQLRequest
        {
            Variables = variables,
            Extensions = new GraphQLRequestExtensions
            {
                PersistedQuery = new GraphQLPersistedQueryExtension
                {
                    Version = 1,
                    SHA256Hash = queryInfo.Hash
                }
            }
        };

        var content = contentCreator(qlRequest);
        var response = await httpClient.PostAsync("", content);
        var qlResponse = await ReadResponse<TQuery>(response);

        if (qlResponse.Errors is null)
        {
            return qlResponse with { Query = FormatPersistedQuery(queryInfo)  };
        }

        if (qlResponse.Errors.All(o => o.Message != "PersistedQueryNotFound"))
        {
            return qlResponse with { Query = FormatPersistedQuery(queryInfo)  };
        }

        qlRequest.Query = queryInfo.Query;
        content = contentCreator(qlRequest);
        response = await httpClient.PostAsync("", content);
        qlResponse = await ReadResponse<TQuery>(response);

        return qlResponse with { Query = FormatPersistedQuery(queryInfo) };
    }

    private static string FormatPersistedQuery(QueryInfo queryInfo)
    {
        return $"{queryInfo.Hash}:{queryInfo.Query}";
    }

    private static async Task<GraphQLResponse<TQuery>> ReadResponse<TQuery>(HttpResponseMessage response)
    {
#if DEBUG
        var responseJson = await response.Content.ReadAsStringAsync();
        var qlResponse = JsonSerializer.Deserialize<GraphQLResponse<TQuery>>(responseJson, ZeroQLJsonOptions.Options);
#endif
#if !DEBUG
        var responseJson = await response.Content.ReadAsStreamAsync();
        var qlResponse = await JsonSerializer.DeserializeAsync<GraphQLResponse<TQuery>>(responseJson, ZeroQLJsonOptions.Options);
#endif
        return qlResponse!;
    }
}