using System;
using System.Threading;
using System.Threading.Tasks;
using ZeroQL;

// ReSharper disable once CheckNamespace
public static class GraphQLClientRequestExtensions
{
    const string ErrorMessage = "The request syntax is not supported in ZeroQL 7+ to make it AOT compatible.";
    
    [Error(ErrorMessage)]
    [Obsolete(ErrorMessage)]
    public static async Task<GraphQLResult<TResult>> Execute<TQuery, TResult>(
        this IGraphQLClient client, GraphQL<TQuery, TResult> request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(ErrorMessage);
    }
}