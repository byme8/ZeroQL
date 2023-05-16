using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ZeroQL.SourceGenerators;

public static class Utils
{
    public static readonly Dictionary<string, string> CSharpToGraphQL = new()
    {
        { "string", "String" },
        { "byte", "Byte" },
        { "Int16", "Short" },
        { "Int32", "Int" },
        { "Int64", "Long" },
        { "Single", "Float" },
        { "Double", "Float" },
        { "Decimal", "Decimal" },
        { "DateTimeOffset", "DateTime" },
        { "DateOnly", "Date" },
        { "Guid", "UUID" },
        { "bool", "Boolean" },
        { "global::ZeroQL.Upload", "Upload" },
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

    public static IEnumerable<IPropertySymbol> GetRealProperties(this ITypeSymbol symbol)
    {
        var members = symbol
            .GetAllMembers()
            .Where(o => !o.IsImplicitlyDeclared && !o.IsStatic && o.DeclaredAccessibility == Accessibility.Public)
            .ToArray();

        foreach (var member in members.OfType<IPropertySymbol>())
        {
            yield return member;
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

    public static string SpaceLeft(this string text, int lenght, int mult = 4)
    {
        var spaces = new string(' ', lenght * mult);
        return $"{spaces}{text}";
    }

    public static string JoinWithNewLine(this IEnumerable<string> values, string separator = "")
    {
        return string.Join($"{separator}{Environment.NewLine}", values);
    }

    public static string ToGraphQLType(this ITypeSymbol typeSymbol)
    {
        return typeSymbol switch
        {
            IArrayTypeSymbol arrayTypeSymbol =>
                $"[{arrayTypeSymbol.ElementType.ToGraphQLType()}]{typeSymbol.Nullable()}",
            INamedTypeSymbol { Name: "Nullable" } namedType => namedType.TypeArguments[0].ToGraphQLType(),
            _ => Map(typeSymbol) + typeSymbol.Nullable(),
        };
    }

    private static string Map(ITypeSymbol typeSymbol)
    {
        return CSharpToGraphQL.ContainsKey(typeSymbol.Name) ? CSharpToGraphQL[typeSymbol.Name] : typeSymbol.Name;
    }

    public static string Nullable(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.NullableAnnotation switch
        {
            NullableAnnotation.None => "!",
            NullableAnnotation.NotAnnotated => "!",
            NullableAnnotation.Annotated => "",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static INamedTypeSymbol? GetNamedTypeSymbol(this ISymbol info)
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
            case IFieldSymbol fieldSymbol:
                return fieldSymbol.Type.GetNamedTypeSymbol();
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

        if (symbol is INamedTypeSymbol { SpecialType: SpecialType.System_Object })
        {
            return "object?";
        }

        if (symbol is IParameterSymbol parameterSymbol)
        {
            return parameterSymbol.Type.ToGlobalName();
        }

        var name = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (string.IsNullOrEmpty(name))
        {
            return symbol.ToDisplayString();
        }

        return name;
    }

    public static string ToSafeGlobalName(this ISymbol symbol)
    {
        var name = symbol.ToGlobalName();

        return name
            .Replace("::", "")
            .Replace("<", "GenerticOf")
            .Replace(">", "")
            .Replace("[", "ArrayOf")
            .Replace("]", "")
            .Replace(" ", "")
            .Replace(",", "")
            .Replace(":", "")
            .Replace(".", "");
    }

    public static void ErrorWrapper(SourceProductionContext context, CSharpSyntaxNode location, Action action)
    {
#if !DEBUG

        try
        {
#endif
            action();
#if !DEBUG
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Descriptors.UnexpectedFail, location.GetLocation(), e.Format()));
        }
#endif
    }

    public static string Format(this Exception exception)
    {
        var sb = new StringBuilder();
        sb.AppendLine(exception.Message);
        sb.AppendLine(exception.StackTrace);
        if (exception.InnerException != null)
        {
            sb.AppendLine("InnerException:");
            sb.AppendLine(exception.InnerException.Format());
        }

        return sb.ToString();
    }
}