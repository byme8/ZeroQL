using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZeroQL.Internal;

namespace ZeroQL.Pipelines;

public interface IGraphQLQueryPipeline
{
    Task<GraphQLResponse<TQuery>> ExecuteAsync<TQuery>(IHttpHandler httpHandler, string queryKey, object? variables, CancellationToken cancellationToken, Func<GraphQLRequest, HttpContent> contentCreator);
}