﻿using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ZeroQL.Internal;
using ZeroQL.Json;
using ZeroQL.Stores;

namespace ZeroQL.Pipelines;

public class FullQueryPipeline : IGraphQLQueryPipeline
{
    public async Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(HttpClient httpClient, string queryKey, object? variables, CancellationToken cancellationToken, Func<GraphQLRequest, HttpContent> contentCreator)
    {
        var queryInfo = GraphQLQueryStore<TQuery>.Query[queryKey];
        var query = queryInfo.Query;
        var qlRequest = new GraphQLRequest
        {
            Query = query,
            Variables = variables
        };

        var content = contentCreator(qlRequest);
        var response = await httpClient.PostAsync("", content, cancellationToken);
        var responseJson = await response.Content.ReadAsStreamAsync(cancellationToken);
        var qlResponse = await JsonSerializer.DeserializeAsync<GraphQLResponse<TQuery>>(responseJson, ZeroQLJsonOptions.Options, cancellationToken);

        if (qlResponse is not null)
        {
            qlResponse.Query = query;
        }

        return qlResponse!;
    }
}