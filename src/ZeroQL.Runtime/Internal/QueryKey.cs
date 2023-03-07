using System;
using System.Linq;

namespace ZeroQL.Internal;

public static class QueryKey
{
    public static string Normalize(string graphqlKey)
    {
        var parts = graphqlKey.Split(
            new[] { '\t', '\r', '\n' }, 
            StringSplitOptions.RemoveEmptyEntries);

        return string.Join(" ", parts.Select(o => o.Trim()));
    }
}