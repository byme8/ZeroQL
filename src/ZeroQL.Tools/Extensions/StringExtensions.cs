using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroQL.Extensions;

public static class StringExtensions
{
    public static string Join(this IEnumerable<string>? values, string separator = ", ")
    {
        if (values is null)
        {
            return string.Empty;
        }

        return string.Join(separator, values);
    }

    public static string JoinWithNewLine(this IEnumerable<string>? values, int gap = 0)
    {
        return values.Join(Environment.NewLine);
    }

    public static string FirstToUpper(this string value)
    {
        return value[..1].ToUpper() + value[1..];
    }

    public static string ToPascalCase(this string value)
    {
        return value
            .Split("_", StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.ToLower().FirstToUpper())
            .Join(string.Empty);
    }
    
    public static string ToUpperCase(this string name)
    {
        var upperCaseString = new List<char>(name.Length);
        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];
            if (i != 0 && char.IsUpper(ch))
            {
                upperCaseString.Add('_');
                upperCaseString.Add(ch);
                continue;
            }

            upperCaseString.Add(char.ToUpper(name[i]));
        }

        return new string(upperCaseString.ToArray());
    }
}