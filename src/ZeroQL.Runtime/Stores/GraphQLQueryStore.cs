using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroQL.Stores;

public static class GraphQLQueryStore<TQuery>
{
    public static Dictionary<string, Func<QueryExecuteContext, Task<GraphQLResult<TQuery>>>> Executor { get; } = new();

    public static Dictionary<string, QueryInfo> Query { get; } = new();
}

public class QueryExecuteContext
{
    public IGraphQLClient Client { get; set; }

    public string QueryKey { get; set; }

    public object? Variables { get; set; }

    public CancellationToken CancellationToken { get; set; }
}

public class QueryInfo
{
    public string Query { get; set; }

    public string OperationType { get; set; }

    public string Hash { get; set; }
}