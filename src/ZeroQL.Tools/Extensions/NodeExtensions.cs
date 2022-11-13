using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.Extensions;

public static class NodeExtensions
{
    public static ClassDeclarationSyntax? GetClass(this SyntaxTree syntaxTree, string name)
    {
        var type = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(o => o.Identifier.ValueText == name);

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