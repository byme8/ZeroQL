using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ZeroQL.Json;

namespace ZeroQL.Pipelines;

public static class HttpResponseMessageExtension
{
    public static async Task<GraphQLResponse<TQuery>> ReadGraphQLResponse<TQuery>(this HttpResponseMessage response,
        HttpRequestMessage request,
        IZeroQLSerializer serialization,
        CancellationToken cancellationToken)
    {
        const string responseContentTypeStartWith = "application/graphql-response";
        var graphqlResponse = response.Content.Headers.ContentType?.MediaType?.StartsWith(responseContentTypeStartWith);
        if (!response.IsSuccessStatusCode && !(graphqlResponse ?? false))
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            return new GraphQLResponse<TQuery>
            {
                HttpResponseMessage = response,
                Errors =
                [
                    new()
                    {
                        Message =
                            $"""HTTP request failed unexpectedly with status code {(int)response.StatusCode}. Look at the Extensions.ZeroQLError extension for more details""",
                    }
                ],
                Extensions = new()
                {
                    ["ZeroQLError"] = new Dictionary<object, object?>
                    {
                        ["Uri"] = request.RequestUri?.ToString(),
                        ["HttpStatusCode"] = (int)response.StatusCode,
                        ["ReasonPhrase"] = response.ReasonPhrase,
                        ["ResponseContent"] = responseContent
                    }
                }
            };
        }

#if DEBUG
        var responseJson = await response.Content.ReadAsStringAsync();
        var qlResponse = serialization.Deserialize<GraphQLResponse<TQuery>>(responseJson);
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

        if (qlResponse is null)
        {
            return new GraphQLResponse<TQuery>
            {
                HttpResponseMessage = response,
                Errors =
                [
                    new()
                    {
                        Message =
                            """Failed to deserialize the response from the server. Look at the Extensions.ZeroQLError extension for more details""",
                    }
                ],
                Extensions = new()
                {
                    ["ZeroQLError"] = new Dictionary<object, object?>
                    {
                        ["Uri"] = request.RequestUri?.ToString(),
                        ["HttpStatusCode"] = (int)response.StatusCode,
                        ["ReasonPhrase"] = response.ReasonPhrase,
                    }
                }
            };
        }

        return qlResponse with
        {
            HttpResponseMessage = response
        };
    }
}