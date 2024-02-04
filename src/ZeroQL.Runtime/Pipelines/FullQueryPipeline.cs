using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZeroQL.Internal;
using ZeroQL.Stores;

namespace ZeroQL.Pipelines;

public class FullQueryPipeline : IGraphQLQueryPipeline
{
    public async Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(
        IHttpHandler httpHandler,
        string queryKey,
        object? variables,
        CancellationToken cancellationToken,
        Func<GraphQLRequest, HttpContent> contentCreator)
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
        var qlResponse = await response.ReadGraphQLResponse<TQuery>(request, cancellationToken);

        qlResponse.Query = query;
        return qlResponse;
    }
}