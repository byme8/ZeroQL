using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser;
using GraphQLParser.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Bootstrap.Generators;
using ZeroQL.Extensions;
using ZeroQL.Internal;
using ZeroQL.Internal.Enums;
using ZeroQL.Schema;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZeroQL.Bootstrap;

public static class GraphQLGenerator
{
    public static string ToCSharp(string graphql, string clientNamespace, string? clientName)
    {
        var options = new GraphQlGeneratorOptions(clientNamespace, ClientVisibility.Public)
        {
            ClientName = clientName
        };

        return ToCSharp(graphql, options);
    }

    public static string ToCSharp(string graphql, GraphQlGeneratorOptions options)
    {
        var schema = Parser.Parse(graphql);
        var enums = schema.Definitions
            .OfType<GraphQLEnumTypeDefinition>()
            .OrderBy(o => o.Name.StringValue)
            .Select(o => new EnumDefinition(
                o.Name.StringValue,
                o.Values?
                    .Select(v => v.Name.StringValue)
                    .ToArray()))
            .ToArray();

        var schemaDefinition = schema.Definitions
            .OfType<GraphQLSchemaDefinition>()
            .FirstOrDefault();

        if (schemaDefinition is null)
        {
            return "// Schema definition not found";
        }

        var queryType = schemaDefinition.OperationTypes
            .FirstOrDefault(x => x.Operation == OperationType.Query)?
            .Type;

        var mutationType = schemaDefinition.OperationTypes
            .FirstOrDefault(x => x.Operation == OperationType.Mutation)?
            .Type;

        var enumsNames = new HashSet<string>(enums.Select(o => o.Name));

        var scalarTypesFromSchema = schema.Definitions
            .OfType<GraphQLScalarTypeDefinition>()
            .OrderBy(o => o.Name.StringValue)
            .Select(o => o.Name.StringValue)
            .ToArray();

        var scalarsToOverride = options.Scalars ?? new Dictionary<string, string>();
        var context = new TypeContext(options, enumsNames, scalarTypesFromSchema, scalarsToOverride);
        var customScalars = context.CustomScalars;

        var inputs = schema.Definitions
            .OfType<GraphQLInputObjectTypeDefinition>()
            .OrderBy(o => o.Name.StringValue)
            .Select(o => CreateInputDefinition(context, o))
            .ToArray();

        var interfaces = schema.Definitions
            .OfType<GraphQLInterfaceTypeDefinition>()
            .OrderBy(o => o.Name.StringValue)
            .Select(o => CreateInterfaceDefinition(context, o))
            .ToDictionary(o => o.Name);

        var types = schema.Definitions
            .OfType<GraphQLObjectTypeDefinition>()
            .OrderBy(o => o.Name.StringValue)
            .Select(o => CreateTypesDefinition(context, interfaces, o))
            .ToArray();


        AddUnions(schema, interfaces, types);

        var namespaceDeclaration = NamespaceDeclaration(IdentifierName(options.ClientNamespace));
        var clientDeclaration = new[] { options.GenerateClient(queryType, mutationType) };
        var typesDeclaration = options.GenerateTypes(types, queryType, mutationType);
        var interfacesDeclaration = options.GenerateInterfaces(interfaces);
        var inputsDeclaration = options.GenerateInputs(inputs);
        var enumsDeclaration = GenerateEnums(options, enums);
        var scalarDeclaration = options.GenerateScalars(customScalars);
        var jsonInitializers = options.GenerateJsonInitializers(customScalars, enums, interfaces.Values);
        var interfaceInitializers = interfaces.GenerateInterfaceInitializers(types);

        var members = List<MemberDeclarationSyntax>(clientDeclaration)
            .AddRange(typesDeclaration)
            .AddRange(interfacesDeclaration)
            .AddRange(inputsDeclaration)
            .AddRange(scalarDeclaration)
            .AddRange(enumsDeclaration)
            .AddRange(interfaceInitializers)
            .Add(jsonInitializers);

        namespaceDeclaration = namespaceDeclaration
            .WithMembers(members);

        var disableWarning = PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)
            .WithErrorCodes(
                SingletonSeparatedList<ExpressionSyntax>(LiteralExpression(SyntaxKind.NumericLiteralExpression,
                    Literal(8618))));
        var restoreWarning = PragmaWarningDirectiveTrivia(Token(SyntaxKind.RestoreKeyword), true)
            .WithErrorCodes(
                SingletonSeparatedList<ExpressionSyntax>(LiteralExpression(SyntaxKind.NumericLiteralExpression,
                    Literal(8618))));

        var namespacesToImport = new[]
        {
            "System",
            "System.Linq",
            "System.Text.Json.Serialization",
            "System.Text.Json.Nodes",
            "System.Text.Json",
            "ZeroQL",
            "ZeroQL.Json",
        };
        var checksum = ChecksumHelper.GenerateChecksumFromInlineSchema(graphql, options);

        namespaceDeclaration = namespaceDeclaration
            .WithLeadingTrivia(
                Comment($"// {checksum}"),
                Comment("// This file generated for ZeroQL."),
                Comment("// <auto-generated/>"),
                Trivia(disableWarning),
                CarriageReturnLineFeed,
                Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)),
                CarriageReturnLineFeed)
            .WithUsings(
                List(namespacesToImport
                    .Select(o => UsingDirective(IdentifierName(o)))))
            .WithTrailingTrivia(
                Trivia(restoreWarning));

        var unit = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(namespaceDeclaration));

        if (options.NetstandardCompatibility is true)
        {
            unit = unit.AddMembers(GenerateNetstandardCompatibility());
        }

        var formattedSource = unit
            .NormalizeWhitespace()
            .ToFullString();

        return formattedSource;
    }

    private static MemberDeclarationSyntax GenerateNetstandardCompatibility()
    {
        var moduleInitializer = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Method, Inherited = false)]
                public sealed class ModuleInitializerAttribute : Attribute
                {
                }
            }
           """;

        var moduleInitializerSyntax = ParseCompilationUnit(moduleInitializer)
            .WithLeadingTrivia(Comment("// Netstandard compatibility"))
            .WithTrailingTrivia(CarriageReturnLineFeed)
            .Members[0];

        return moduleInitializerSyntax;
    }

    private static ClassDefinition CreateTypesDefinition(
        TypeContext typeContext,
        Dictionary<string, InterfaceDefinition> interfaces,
        GraphQLObjectTypeDefinition type)
    {
        var typeInterfaces = type.Interfaces?
            .Select(o => interfaces[o.Name.StringValue])
            .ToList() ?? new List<InterfaceDefinition>();

        var classFields = new List<FieldDefinition>();
        var classDefinition = new ClassDefinition(type.Name.StringValue, classFields, typeInterfaces);
        var fields = typeContext.CreatePropertyDefinition(classDefinition, type.Fields);
        classDefinition = VerifyDefinition(fields, classDefinition);
        classFields.AddRange(fields);

        return classDefinition;
    }

    private static TDefinition VerifyDefinition<TDefinition>(FieldDefinition[] fields, TDefinition definition)
        where TDefinition : Definition
    {
        if (fields.Any(o => o.Name == definition.Name))
        {
            definition = definition with { Name = $"{definition.Name}_ZeroQL" };
        }

        return definition;
    }

    private static InterfaceDefinition CreateInterfaceDefinition(TypeContext typeContext,
        GraphQLInterfaceTypeDefinition definition)
    {
        var interfaceFields = new List<FieldDefinition>();
        var interfaceDefinition = new InterfaceDefinition(definition.Name.StringValue, interfaceFields);
        var fields = typeContext.CreatePropertyDefinition(interfaceDefinition, definition.Fields);
        interfaceDefinition = VerifyDefinition(fields, interfaceDefinition);
        interfaceFields.AddRange(fields);

        return interfaceDefinition;
    }

    public static UnionDefinition CreateUnionDefinition(GraphQLUnionTypeDefinition union)
        => new(union.Name.StringValue,
            union.Types?.Select(o => o.Name.StringValue).ToArray() ?? Array.Empty<string>());

    private static ClassDefinition CreateInputDefinition(TypeContext typeContext,
        GraphQLInputObjectTypeDefinition input)
    {
        var classFields = new List<FieldDefinition>();
        var classDefinition = new ClassDefinition(input.Name.StringValue, classFields, new List<InterfaceDefinition>());
        var fields = typeContext.CreatePropertyDefinition(classDefinition, input.Fields);
        classDefinition = VerifyDefinition(fields, classDefinition);
        classFields.AddRange(fields);

        return classDefinition;
    }

    private static void AddUnions(GraphQLDocument schema,
        Dictionary<string, InterfaceDefinition> interfaces,
        ClassDefinition[] types)
    {
        var unions = schema.Definitions
            .OfType<GraphQLUnionTypeDefinition>()
            .OrderBy(o => o.Name.StringValue)
            .Select(CreateUnionDefinition)
            .ToArray();

        foreach (var union in unions)
        {
            var unionInterface = new InterfaceDefinition(union.Name, Array.Empty<FieldDefinition>());
            interfaces.Add(union.Name, unionInterface);
            foreach (var unionType in union.Types)
            {
                var type = types.FirstOrDefault(o => o.Name == unionType);
                if (type is not null)
                {
                    type.Implements.Add(unionInterface);
                }
            }
        }
    }

    private static EnumDeclarationSyntax[] GenerateEnums(
        GraphQlGeneratorOptions options,
        EnumDefinition[] enums)
    {
        return enums.Select(e =>
            {
                var members = e.Values?.Select(o =>
                    {
                        var name = o.ToPascalCase();
                        return EnumMemberDeclaration(Identifier(name))
                            .AddAttribute(ZeroQLGenerationInfo.GraphQLFieldSelectorAttribute, o);
                    })
                    .ToArray() ?? Array.Empty<EnumMemberDeclarationSyntax>();

                var enumSyntax = EnumDeclaration(Identifier(e.Name))
                    .AddAttributeLists(AttributeList()
                        .AddAttributes(Attribute(ParseName(ZeroQLGenerationInfo.CodeGenerationAttribute))))
                    .AddMembers(members)
                    .AddModifiers(Token(
                        options.Visibility == ClientVisibility.Public
                            ? SyntaxKind.PublicKeyword
                            : SyntaxKind.InternalKeyword));

                return enumSyntax;
            })
            .ToArray();
    }
}