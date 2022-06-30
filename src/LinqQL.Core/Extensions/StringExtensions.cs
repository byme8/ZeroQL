using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LinqQL.Core.Extensions;

public static class NodeExtensions
{
    public static ClassDeclarationSyntax GetClass(this SyntaxTree syntaxTree, string name)
    {
        var type = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(o => o.Identifier.ValueText == name);

        return type;
    }
    
    public static MethodDeclarationSyntax GetMethod(this ClassDeclarationSyntax @class, string name)
    {
        return @class.Members
            .OfType<MethodDeclarationSyntax>()
            .First(o => o.Identifier.ValueText == name);
    }
    
    public static PropertyDeclarationSyntax GetProperty(this ClassDeclarationSyntax @class, string name)
    {
        return @class.Members
            .OfType<PropertyDeclarationSyntax>()
            .First(o => o.Identifier.ValueText == name);
    }
}

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

    public static IEnumerable<string>? Level(this IEnumerable<string>? values, int spaces)
    {
        var lines = values?.SelectMany(o => o.Split(Environment.NewLine));
        return lines?.Select(o => new string(' ', spaces * 4) + o);
    }

    public static string JoinWithNewLine(this IEnumerable<string>? values, int gap = 0)
    {
        return values.Join(Environment.NewLine);
    }

    public static string FirstToUpper(this string value)
    {
        return value[..1].ToUpper() + value[1..];
    }
}