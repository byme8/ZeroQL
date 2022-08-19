using System;
using System.Collections.Generic;
using System.Linq;
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

    public static IEnumerable<(string Name, ITypeSymbol Type)> GetRealProperties(this ITypeSymbol symbol)
    {
        var members = symbol
            .GetAllMembers()
            .Where(o => !o.IsImplicitlyDeclared && !o.IsStatic && o.DeclaredAccessibility == Accessibility.Public)
            .ToArray();

        foreach (var member in members.OfType<IPropertySymbol>())
        {
            yield return (member.Name, member.Type);
        }
    }

    public static IEnumerable<ITypeSymbol> GetAllBaseTypes(this ITypeSymbol symbol)
    {
        if (symbol.BaseType != null)
        {
            foreach (var baseType in symbol.BaseType.GetAllBaseTypes())
            {
                yield return baseType;
            }
        }

        yield return symbol;
    }

    public static string FirstToLower(this string text)
    {
        var chars = text.ToCharArray();
        chars[0] = char.ToLower(chars[0]);

        return new string(chars);
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

    public static INamedTypeSymbol GetNamedTypeSymbol(this ISymbol info)
    {
        switch (info)
        {
            case INamedTypeSymbol type:
                return type;
            case ILocalSymbol local:
                return local.Type.GetNamedTypeSymbol();
            case IParameterSymbol parameterSymbol:
                return parameterSymbol.Type.GetNamedTypeSymbol();
            default:
                return null;
        }
    }

    public static INamedTypeSymbol? GetTypeSymbol(this SymbolInfo info)
    {
        switch (info.Symbol)
        {
            case INamedTypeSymbol type:
                return type;
            case ILocalSymbol local:
                return local.Type.GetNamedTypeSymbol();
            case IParameterSymbol parameterSymbol:
                return parameterSymbol.Type.GetNamedTypeSymbol();
            default:
                return null;
        }
    }

    public static string ToGlobalName(this ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol { IsTupleType: true } anonymousType)
        {
            return anonymousType.ToDisplayString();
        }

        if (symbol is INamedTypeSymbol { SpecialType: SpecialType.System_Object } namedTypeSymbol)
        {
            return "object?";
        }


        return $"global::{symbol.ToDisplayString()}";
    }
}