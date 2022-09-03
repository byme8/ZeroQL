using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroQL.Stores;

public static class GraphQLQueryStore<TQuery>
{
    public static Dictionary<string, Func<IGraphQLClient, string, string?, object?, Task<GraphQLResult<TQuery>>>> Executor { get; } = new();

    public static Dictionary<string, QueryInfo> Query { get; } = new();
}

public class QueryInfo
{
    public string QueryBody { get; set; }

    public string OperationType { get; set; }

    public string Hash { get; set; }
}