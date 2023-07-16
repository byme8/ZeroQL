using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Internal.Enums;
using ZeroQL.Schema;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using TypeDefinition = ZeroQL.Schema.TypeDefinition;

namespace ZeroQL.Internal;

internal static class CSharpHelper
{
    public static ClassDeclarationSyntax Class(string name, ClientVisibility visibility)
    {
        return ClassDeclaration(name)
            .AddModifiers(ParseToken(visibility == ClientVisibility.Public ? "public" : "internal"));
    }

    public static InterfaceDeclarationSyntax Interface(string name, ClientVisibility visibility)
    {
        return InterfaceDeclaration(name)
            .AddModifiers(ParseToken(visibility == ClientVisibility.Public ? "public" : "internal"));
    }

    public static T AddAttributes<T>(this T declarationSyntax, params string[] attributes)
        where T : TypeDeclarationSyntax
    {
        return (T)declarationSyntax
            .AddAttributeLists(AttributeList()
                .AddAttributes(attributes
                    .Select(o => Attribute(ParseName(o)))
                    .ToArray()));
    }

    public static T AddAttributeWithStringParameter<T>(this T declarationSyntax, string name, params string[] arguments)
        where T : MemberDeclarationSyntax
    {
        var attribute = Attribute(ParseName(name));
        if (arguments.Length > 0)
        {
            attribute = attribute
                .WithArgumentList(AttributeArgumentList()
                    .WithArguments(
                        SeparatedList(arguments
                            .Select(o => AttributeArgument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(o))))
                            .ToArray())));
        }

        return (T)declarationSyntax
            .AddAttributeLists(AttributeList()
                .AddAttributes(attribute));
    }

    public static PropertyDeclarationSyntax Property(string name, TypeDefinition type, string? defaultValue)
    {
        var fullTypeName = GetPropertyType(type, false);

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
    
    public static string GetPropertyType(TypeDefinition typeDefinition, bool generic)
    {
        switch (typeDefinition)
        {
            case ObjectTypeDefinition type:
                var typeName = generic ? "T" : type.Name;
                return typeName + type.NullableAnnotation();
            case ScalarTypeDefinition type:
                return type.NameWithNullableAnnotation();
            case EnumTypeDefinition type:
                return type.NameWithNullableAnnotation();
            case ListTypeDefinition type:
                return $"{GetPropertyType(type.ElementTypeDefinition, generic)}[]{type.NullableAnnotation()}";
            default:
                throw new NotImplementedException();
        }
    }

    private static ExpressionSyntax? GetInitializerExpression(TypeDefinition type, string? strValue) => type.Name switch
    {
        "float" => string.IsNullOrEmpty(strValue)
            ? null
            : LiteralExpression(SyntaxKind.NumericLiteralExpression,
                Literal(double.Parse(strValue, CultureInfo.InvariantCulture))),
        "string" => string.IsNullOrEmpty(strValue)
            ? null
            : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(strValue)),
        "int" => string.IsNullOrEmpty(strValue)
            ? null
            : LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(int.Parse(strValue))),
        "bool" => string.IsNullOrEmpty(strValue)
            ? null
            : LiteralExpression(bool.Parse(strValue)
                ? SyntaxKind.TrueLiteralExpression
                : SyntaxKind.FalseLiteralExpression),

        //ID is always represented as a string in client-server communication. REF: https://chillicream.com/docs/hotchocolate/v12/defining-a-schema/scalars#id
        "ID" => string.IsNullOrEmpty(strValue)
            ? null
            : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(strValue)),

        _ => null
    };

    public static T AddAttributeWithRawParameters<T>(
        this T member, string name, string arguments)
        where T : MemberDeclarationSyntax
    {
        return (T)member
            .AddAttributeLists(AttributeList()
                .AddAttributes(Attribute(ParseName(name), ParseAttributeArgumentList($"({arguments})"))));
    }

    public static string EnsureNotKeyword(this string identifier)
    {
        if (SyntaxFacts.GetKeywordKind(identifier) is not SyntaxKind.None)
            return $"@{identifier}";

        return identifier;
    }
    
    public static T WithComment<T>(this T member, string comment)
        where T : CSharpSyntaxNode
    {
        return member.WithLeadingTrivia(
            ParseLeadingTrivia(comment));
    }
}