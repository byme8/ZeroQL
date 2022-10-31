using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ZeroQL.Internal;
using ZeroQL.Json;
using ZeroQL.Stores;

namespace ZeroQL.Pipelines;

public class PersistedQueryPipeline : IGraphQLQueryPipeline
{
    public PersistedQueryPipeline(bool tryToAddPersistedQueryOnFail = true)
    {
        TryToAddPersistedQueryOnFail = tryToAddPersistedQueryOnFail;
    }

    public bool TryToAddPersistedQueryOnFail { get; }

    public async Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(IGraphQLTransport transport, string queryKey, object? variables, Func<GraphQLRequest, IGraphQLTransportContent> contentCreator)
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

        var qlResponse = await transport.DeliverAsync<TQuery>(qlRequest.Query, content);

        if (qlResponse.Errors is null)
        {
            return qlResponse with { Query = FormatPersistedQuery(queryInfo) };
        }

        if (!TryToAddPersistedQueryOnFail)
        {
            return qlResponse with { Query = FormatPersistedQuery(queryInfo) };
        }

        if (qlResponse.Errors.All(o => o.Message != "PersistedQueryNotFound"))
        {
            return qlResponse with { Query = FormatPersistedQuery(queryInfo) };
        }

        qlRequest.Query = queryInfo.Query;
        content = contentCreator(qlRequest);
        qlResponse = qlResponse = await transport.DeliverAsync<TQuery>(qlRequest.Query, content);

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