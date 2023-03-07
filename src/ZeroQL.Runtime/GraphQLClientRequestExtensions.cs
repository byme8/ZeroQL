using System.Threading;
using System.Threading.Tasks;
using ZeroQL;

// ReSharper disable once CheckNamespace
public static class GraphQLClientRequestExtensions
{
    public static async Task<GraphQLResult<TResult>> Execute<TQuery, TResult>(
        this IGraphQLClient client, GraphQL<TQuery, TResult> request, CancellationToken cancellationToken = default)
    {
        var type = request.GetType();
        return await client.Execute<GraphQL<TQuery, TResult>, TQuery, TResult>(
            request,
            (_, q) => request.Execute(q),
            type.FullName!,
            cancellationToken);
    }
}