using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZeroQL.Internal;
using ZeroQL.Stores;

namespace ZeroQL.Pipelines;

public class FullQueryPipeline(IZeroQLSerializer serialization) : IGraphQLQueryPipeline
{
    public async Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(IHttpHandler httpHandler, string queryKey,
        object? variables, CancellationToken cancellationToken, Func<GraphQLRequest, HttpContent> contentCreator)
    {
        var queryInfo = GraphQLQueryStore<TQuery>.Query[queryKey];
        var query = queryInfo.Query;
        var qlRequest = new GraphQLRequest
        {
            Query = query,
            Variables = variables
        };

        var content = contentCreator(qlRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri("", UriKind.Relative));
        request.Content = content;
        var response = await httpHandler.SendAsync(request, cancellationToken);
#if DEBUG
        var responseJson = await response.Content.ReadAsStringAsync();
        var bytes = System.Text.Encoding.UTF8.GetBytes(responseJson);
        var qlResponse = serialization.Deserialize<GraphQLResponse<TQuery>>(bytes);
#elif NETSTANDARD
        var responseJson = await response.Content.ReadAsStreamAsync();
        var qlResponse = await serialization.Deserialize<GraphQLResponse<TQuery>>(
            responseJson,
            cancellationToken);
#else
        var responseJson = await response.Content.ReadAsStreamAsync(cancellationToken);
        var qlResponse = await serialization.Deserialize<GraphQLResponse<TQuery>>(
                responseJson,
                cancellationToken);
#endif

        if (qlResponse is not null)
        {
            qlResponse.Query = query;
        }

        return qlResponse!;
    }
}