using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using ZeroQL.Internal;
using ZeroQL.Json;
using ZeroQL.Stores;

namespace ZeroQL.Pipelines;

public class FullQueryPipeline : IGraphQLQueryPipeline
{
    public static MessagePackSerializerOptions _options = MessagePackSerializerOptions.Standard.WithResolver(
        CompositeResolver.Create(
            new IMessagePackFormatter[] { new StringInterningFormatter() },
            new IFormatterResolver[]
            {
                StandardResolver.Instance,
            }));
    
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
        request.Headers.Add("Accept", "application/msgpack");
        var response = await httpHandler.SendAsync(request, cancellationToken);
// #if DEBUG
        var bytes = await response.Content.ReadAsByteArrayAsync();
        // var json = MessagePackSerializer.ConvertToJson(bytes);
        // var a  = MessagePackSerializer.Deserialize<GraphQLResponse<TQuery>>(bytes, options);
        var qlResponse = MessagePackSerializer.Deserialize<GraphQLResponse<TQuery>>(bytes, _options);
// #elif NETSTANDARD
//         var responseJson = await response.Content.ReadAsStreamAsync();
//         var qlResponse = await JsonSerializer.DeserializeAsync<GraphQLResponse<TQuery>>(
//             responseJson,
//             ZeroQLJsonOptions.Options,
//             cancellationToken);
// #else
         // var responseJson = await response.Content.ReadAsStreamAsync();
         // var qlResponse = await JsonSerializer.DeserializeAsync<GraphQLResponse<TQuery>>(
         //         responseJson,
         //         ZeroQLJsonOptions.Options,
         //         cancellationToken);
// #endif

        if (qlResponse is not null)
        {
            qlResponse.Query = query;
        }

        return qlResponse!;
    }
}