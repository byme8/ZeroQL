using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

    public static EnumDeclarationSyntax? GetEnum(this SyntaxTree syntaxTree, string name)
    {
        var type = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<EnumDeclarationSyntax>()
            .FirstOrDefault(o => o.Identifier.ValueText == name);

        return type;
    }

    public static InterfaceDeclarationSyntax? GetInterface(this SyntaxTree syntaxTree, string name)
    {
        var type = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>()
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

    public static ParameterSyntax AddForcedDefault(this ParameterSyntax argument)
    {
        return argument.WithDefault(
            EqualsValueClause(
                PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression,
                    LiteralExpression(SyntaxKind.DefaultLiteralExpression))));
    }
}