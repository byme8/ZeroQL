using System;
using System.Runtime.CompilerServices;
using ZeroQL.Internal;

namespace ZeroQL.Stores;

public class QueryInfo
{
    public string Query { get; set; }

    public string QueryBody { get; set; }

    public string OperationType { get; set; }

    public string Hash { get; set; }
}

public class QueryInfoProvider
{
    public static QueryInfo Materialize<TQuery>(
        Func<TQuery, object> query,
        [CallerArgumentExpression(nameof(query))] string queryKey = "")
    {
        var normalizedKey = QueryKey.Normalize(queryKey);
        if (!GraphQLQueryStore<TQuery>.Query.TryGetValue(normalizedKey, out var queryInfo))
        {
            throw new InvalidOperationException("Query is not bootstrapped.");
        }

        return queryInfo;
    }
}