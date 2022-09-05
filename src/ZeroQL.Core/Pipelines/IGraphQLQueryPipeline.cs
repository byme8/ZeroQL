using System;
using System.Net.Http;
using System.Threading.Tasks;
using ZeroQL.Internal;

namespace ZeroQL.Pipelines;

public interface IGraphQLQueryPipeline
{
    Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(HttpClient httpClient, string queryKey, object? variables, Func<GraphQLRequest, HttpContent> contentCreator);
}