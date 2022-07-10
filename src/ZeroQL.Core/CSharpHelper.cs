using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZeroQL.Core;

public static class CSharpHelper
{
    public static ClassDeclarationSyntax Class(string name)
    {
        return ClassDeclaration(name)
            .AddModifiers(ParseToken("public"));
    }

    public static ClassDeclarationSyntax AddAttributes(this ClassDeclarationSyntax classDeclarationSyntax, params string[] attributes)
    {
        return classDeclarationSyntax
            .AddAttributeLists(AttributeList()
                .AddAttributes(attributes
                    .Select(o => Attribute(ParseName(o)))
                    .ToArray()));
    }

    public static PropertyDeclarationSyntax Property(string name, string type)
    {
        return PropertyDeclaration(ParseTypeName(type), Identifier(name))
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(ParseToken(";")),
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(ParseToken(";")));
    }
    
    public static PropertyDeclarationSyntax AddAttributes(this PropertyDeclarationSyntax classDeclarationSyntax, params (string Name, string Arguments)[] attributes)
    {
        return classDeclarationSyntax
            .AddAttributeLists(AttributeList()
                .AddAttributes(attributes
                    .Select(o => Attribute(ParseName(o.Name), ParseAttributeArgumentList($"({o.Arguments})")))
                    .ToArray()));
    }
}