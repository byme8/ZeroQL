using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroQL.Extensions;

public static class StringExtensions
{
    public static string SpaceLeft(this string text, int length, int mult = 4)
    {
        var spaces = new string(' ', length * mult);
        var lines = text.Split(new []{ '\r', '\n' });
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"{spaces}{line}");
            }
        }
        
        return sb.ToString().TrimEnd();
    }
    
    public static string Join(this IEnumerable<string>? values, string separator = ", ")
    {
        if (values is null)
        {
            return string.Empty;
        }

        return string.Join(separator, values);
    }

    public static string JoinWithNewLine(this IEnumerable<string> values, string separator = "")
    {
        return string.Join($"{separator}{Environment.NewLine}", values);
    }

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
        
        return value
            .Split("_", StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.ToLower().FirstToUpper())
            .Join(string.Empty);
    }
}