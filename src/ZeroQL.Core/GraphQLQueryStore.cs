using System.Collections.Generic;

namespace ZeroQL.Core;

public static class GraphQLQueryStore
{
    public static Dictionary<string, string> Query { get; } = new();
}