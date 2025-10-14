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
        return values.Join("\n");
    }

#if !NETSTANDARD
    public static string FirstToUpper(this string value)
    {
        return value[..1].ToUpper() + value[1..];
    }

    public static string ToPascalCase(this string value)
    {
        if (value.Any(o => char.IsLower(o) && char.IsLetter(o)))
        {
            return value.FirstToUpper();
        }
        
        var pascalCaseName = value
            .Split(['_'], StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.ToLower().FirstToUpper())
            .Join(string.Empty);

        if (char.IsDigit(pascalCaseName[0]))
        {
            pascalCaseName = "_" + pascalCaseName;
        }
        
        return pascalCaseName;
    }
#endif

    public static string NormalizeLineEndings(this string source)
    {
        return source.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}