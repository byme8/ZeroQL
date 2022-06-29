using System;
using System.Linq;
using GraphQLParser;
using GraphQLParser.AST;
using LinqQL.Core.Extensions;
using LinqQL.Core.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using TypeKind = LinqQL.Core.Schema.TypeKind;

namespace LinqQL.Core.Bootstrap;

public static class GraphQLGenerator
{
    public static string ToCSharp(string graphql, string clientNamespace)
    {
        var context = new TypeFormatter();
        var schema = Parser.Parse(graphql);
        var classes = schema.Definitions
            .OfType<GraphQLObjectTypeDefinition>()
            .Select(o => CreateClassDefinition(context, o))
            .ToArray();


        var namespaceDeclaration = NamespaceDeclaration(IdentifierName(clientNamespace));
        var classesDeclaration = classes
            .Select(o =>
            {
                var backedFields = o.Properties
                    .Where(property => property.TypeKind == TypeKind.Object)
                    .Select(property =>
                    {
                        var jsonNameAttributes =
                            AttributeList(
                                SingletonSeparatedList(
                                    Attribute(
                                            IdentifierName("JsonPropertyName"))
                                        .AddArgumentListArguments(
                                            AttributeArgument(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal(property.Name))))));
                        
                        return FieldDeclaration(
                            VariableDeclaration(ParseTypeName(property.TypeName))
                                .AddVariables(VariableDeclarator("_" + property.Name)))
                            .AddAttributeLists(jsonNameAttributes);
                    });

                var fields = o.Properties.Select(GeneratePropertiesDeclarations);

                return ClassDeclaration(o.Name)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .WithMembers(List<MemberDeclarationSyntax>(backedFields).AddRange(fields));
            })
            .ToArray();

        namespaceDeclaration = namespaceDeclaration
            .WithMembers(List<MemberDeclarationSyntax>(classesDeclaration));

        var formattedSource = namespaceDeclaration.NormalizeWhitespace().ToFullString();
        return "using System.Text.Json.Serialization;\n\n" + formattedSource;
    }

    private static MemberDeclarationSyntax GeneratePropertiesDeclarations(FieldDefinition field)
    {
        if (field.TypeKind == TypeKind.Object)
        {
            var parameters = field.Arguments
                .Select(o =>
                    Parameter(Identifier(o.Name))
                        .WithType(ParseTypeName(o.TypeName)))
                .ToArray();

            var selectorParameter = Parameter(Identifier("selector"))
                .WithType(ParseTypeName($"Func<{field.TypeName}, T>"));

           

            var genericMethodWithType = MethodDeclaration(
                    IdentifierName("T"),
                    Identifier(field.Name + "<T>"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithParameterList(
                    ParameterList(
                        SeparatedList(parameters)
                            .Add(selectorParameter)));

            var body = Block(
                ReturnStatement(
                    InvocationExpression(IdentifierName("selector"))
                        .AddArgumentListArguments(Argument(IdentifierName("_" + field.Name)))));

            return genericMethodWithType
                .WithBody(body);
        }

        return PropertyDeclaration(ParseTypeName(field.TypeName), Identifier(field.Name))
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(ParseToken(";")),
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(ParseToken(";")));
    }

    private static ClassDefinition CreateClassDefinition(TypeFormatter typeFormatter, GraphQLObjectTypeDefinition type)
    {
        var typeDefinition = new ClassDefinition
        {
            Name = type.Name.StringValue,
            Properties = CretePropertyDefinition(typeFormatter, type),
        };

        return typeDefinition;
    }

    private static FieldDefinition[] CretePropertyDefinition(TypeFormatter typeFormatter, GraphQLObjectTypeDefinition typeQL)
    {
        return typeQL.Fields?.Select(field =>
            {
                var type = typeFormatter.GetTypeDefinition(field.Type);
                return new FieldDefinition
                {
                    Name = field.Name.StringValue.FirstToUpper(),
                    TypeName = type.Name,
                    TypeKind = type.TypeKind,
                    Arguments = field.Arguments?
                        .Select(arg => new ArgumentDefinition { Name = arg.Name.StringValue, TypeName = typeFormatter.GetTypeDefinition(arg.Type).Name })
                        .ToArray() ?? Array.Empty<ArgumentDefinition>()
                };
            })
            .ToArray() ?? Array.Empty<FieldDefinition>();
    }
}