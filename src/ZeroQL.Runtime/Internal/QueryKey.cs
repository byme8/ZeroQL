using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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
    
    public static string ComputeHash(string queryBody)
    {
        using var sha256 = SHA256.Create();
        var body = Encoding.UTF8.GetBytes(queryBody);
        var bytes = sha256.ComputeHash(body);

        var builder = new StringBuilder();
        foreach (var t in bytes)
        {
            builder.Append(t.ToString("x2"));
        }

        return builder.ToString();
    }
}