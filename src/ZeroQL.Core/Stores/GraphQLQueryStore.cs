using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZeroQL.Core.Stores;

public static class GraphQLQueryStore<TQuery>
{
    public static Dictionary<string, Func<HttpClient, string?, object, Task<GraphQLResult<TQuery>>>> Query { get; } = new();
}