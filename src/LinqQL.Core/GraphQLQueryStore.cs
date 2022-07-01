using System.Collections.Generic;

namespace LinqQL.Core;

public static class GraphQLQueryStore
{
    public static Dictionary<string, string> Query { get; } = new();
}