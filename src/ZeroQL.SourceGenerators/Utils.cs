using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace ZeroQL.SourceGenerators;

public static class Utils
{
    private static readonly Dictionary<string, string> CSharpToGraphQL = new()
    {
        { "Int32", "Int" },
        { "string", "String" }
    };

    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol)
    {
        if (symbol.BaseType != null)
        {
            foreach (var member in symbol.BaseType.GetAllMembers())
            {
                yield return member;
            }
        }

        foreach (var member in symbol.GetMembers())
        {
            yield return member;
        }
    }

    public static string FirstToLower(this string text)
    {
        var first = text.Substring(0, 1);
        var tail = text.Substring(1);

        return first.ToLower() + tail;
    }

    internal static string ToUpperCase(this string name)
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

    public static string Join(this IEnumerable<string> values, string separator = ", ")
    {
        return string.Join(separator, values);
    }

    public static string Wrap(this string text, string left = "", string right = "")
    {
        return $"{left}{text}{right}";
    }

    public static string JoinWithNewLine(this IEnumerable<string> values, string separator = "")
    {
        return string.Join($"{separator}{Environment.NewLine}", values);
    }

    public static string ToStringWithNullable(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.NullableAnnotation switch
        {
            NullableAnnotation.None => Map(typeSymbol.Name) + "!",
            NullableAnnotation.NotAnnotated => Map(typeSymbol.Name) + "!",
            NullableAnnotation.Annotated => Map(typeSymbol.Name),
            _ => throw new ArgumentOutOfRangeException()
        };

        string Map(string name)
        {
            if (CSharpToGraphQL.ContainsKey(name))
            {
                return CSharpToGraphQL[name];
            }

            return name;
        }
    }
}