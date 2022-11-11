using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Schema;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using TypeDefinition = ZeroQL.Schema.TypeDefinition;

namespace ZeroQL.Internal;

internal static class CSharpHelper
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

    public static PropertyDeclarationSyntax Property(string name, TypeDefinition type, bool withNullableAnnotation, string? defaultValue)
    {
        var fullTypeName = withNullableAnnotation ? type.NameWithNullableAnnotation() : type.Name;

        var propertyDeclarationSyntax = PropertyDeclaration(ParseTypeName(fullTypeName), Identifier(name))
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(ParseToken(";")),
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(ParseToken(";")));

        var initializerExpression = GetInitializerExpression(type, defaultValue);
        if (initializerExpression is not null)
        {
            propertyDeclarationSyntax = propertyDeclarationSyntax
                .WithInitializer(EqualsValueClause(initializerExpression))
                .WithSemicolonToken
                (
                    Token(SyntaxKind.SemicolonToken)
                );
        }

        return propertyDeclarationSyntax;
    }


    private static ExpressionSyntax? GetInitializerExpression(TypeDefinition type, string? strValue) => type.Name switch
    {
        "float"     => string.IsNullOrEmpty(strValue) ? null : LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(double.Parse(strValue))),
        "string"    => string.IsNullOrEmpty(strValue) ? null : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(strValue)),
        "int"       => string.IsNullOrEmpty(strValue) ? null : LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(int.Parse(strValue))),
        "bool"      => string.IsNullOrEmpty(strValue) ? null : LiteralExpression(bool.Parse(strValue) ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),

        //ID is always represented as a string in client-server communication.  REF: https://chillicream.com/docs/hotchocolate/v12/defining-a-schema/scalars#id
        "ID"        => string.IsNullOrEmpty(strValue) ? null : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(strValue)),

        _ => null
    };


    public static PropertyDeclarationSyntax AddAttributes(this PropertyDeclarationSyntax classDeclarationSyntax, params (string Name, string Arguments)[] attributes)
    {
        return classDeclarationSyntax
            .AddAttributeLists(AttributeList()
                .AddAttributes(attributes
                    .Select(o => Attribute(ParseName(o.Name), ParseAttributeArgumentList($"({o.Arguments})")))
                    .ToArray()));
    }
}