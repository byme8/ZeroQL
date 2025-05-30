using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
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

    public async Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(IHttpHandler httpHandler, string queryKey, object? variables, CancellationToken cancellationToken, Func<GraphQLRequest, HttpContent> contentCreator)
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
                    Sha256Hash = queryInfo.Hash
                }
            }
        };

        var content = contentCreator(qlRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri("", UriKind.Relative));
        request.Content = content;
        var response = await httpHandler.SendAsync(request, cancellationToken);
        var qlResponse = await response.ReadGraphQLResponse<TQuery>(request, cancellationToken);

        if (qlResponse.Errors is null)
        {
            return qlResponse with { Query = FormatPersistedQuery(queryInfo) };
        }

        if (!TryToAddPersistedQueryOnFail)
        {
            return qlResponse with { Query = FormatPersistedQuery(queryInfo) };
        }

        if (!qlResponse.Errors.All(FailedToFindPersistedQuery))
        {
            return qlResponse with { Query = FormatPersistedQuery(queryInfo) };
        }

        qlRequest.Query = queryInfo.Query;
        content = contentCreator(qlRequest);
        request = new HttpRequestMessage(HttpMethod.Post, new Uri("", UriKind.Relative));
        request.Content = content;
        response = await httpHandler.SendAsync(request, cancellationToken);
        qlResponse = await ReadResponse<TQuery>(response);

        return qlResponse with { Query = FormatPersistedQuery(queryInfo) };
    }

    private static bool FailedToFindPersistedQuery(GraphQueryError o)
    {
        var hotChocolateV13Way = o.Message == "PersistedQueryNotFound";
        if (hotChocolateV13Way)
        {
            return true;
        }
        
        if (o.Extensions is null)
        {
            return false;
        }
        
        // var hotChocolateV15Way = o.Extensions.TryGetValue("code", out var code);
        // var codeString = code?.ToString();
        // if (hotChocolateV15Way && (codeString?.Equals("HC0020") ?? false))
        // {
        //     return true;
        // }
        
        var hotChocolateV14Way = o.Extensions.ContainsKey("HC0020");
        return hotChocolateV14Way;
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