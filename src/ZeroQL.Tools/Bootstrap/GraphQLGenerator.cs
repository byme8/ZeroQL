using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser;
using GraphQLParser.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using ZeroQL.Bootstrap.Generators;
using ZeroQL.Core.Enums;
using ZeroQL.Extensions;
using ZeroQL.Internal;
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

        var (queryType, mutationType) = GetQueryAndMutation(schema);
        var enumsNames = new HashSet<string>(enums.Select(o => o.Name));

        var scalarTypesFromSchema = schema.Definitions
            .OfType<GraphQLScalarTypeDefinition>()
            .OrderBy(o => o.Name.StringValue)
            .Select(o => o.Name.StringValue)
            .ToArray();

        var scalarsToOverride = options.Scalars ?? new Dictionary<string, string>();
        var context = new TypeContext(options, enumsNames, scalarTypesFromSchema, scalarsToOverride);
        var customScalars = context.CustomScalars;

        var inputDefinitions = schema.Definitions
            .OfType<GraphQLInputObjectTypeDefinition>()
            .OrderBy(o => o.Name.StringValue)
            .ToArray();

        var inputs = inputDefinitions
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

        var clientDeclaration = options.GenerateClient(queryType, mutationType);
        var typesDeclaration = options.GenerateTypes(types, queryType, mutationType);
        var interfacesDeclaration = options.GenerateInterfaces(interfaces);
        var inputsDeclaration = options.GenerateInputs(inputs);
        var enumsDeclaration = GenerateEnums(options, enums);
        var scalarDeclaration = options.GenerateScalars(customScalars);
        var (interfaceInitializers, interfacesForSerialization) =
            interfaces.GenerateInterfaceInitializers(options, types);

        var warningCodes = options.WarningsToIgnore ?? new[] { "8618", "CS8603", "CS1066" };

        var members = new List<IEnumerable<string>>()
        {
            new[] { clientDeclaration },
            typesDeclaration.Select(o => o.NormalizeWhitespace().ToFullString()),
            interfacesDeclaration.Select(o => o.NormalizeWhitespace().ToFullString()),
            inputsDeclaration.Select(o => o.NormalizeWhitespace().ToFullString()),
            scalarDeclaration.Select(o => o.NormalizeWhitespace().ToFullString()),
            enumsDeclaration.Select(o => o.NormalizeWhitespace().ToFullString()),
            interfaceInitializers.Select(o => o.NormalizeWhitespace().ToFullString())
        };

        var checksum = ChecksumHelper.GenerateChecksumFromInlineSchema(graphql, options);
        var fixedRoot = CreateFile(options, checksum, warningCodes, members);

        var graphQLParameters = GetUniqueGraphQLParameters(context, fixedRoot);

        var typesForJsonContext = graphQLParameters
            .Concat(new[]
            {
                "ZeroQL.Internal.GraphQLRequest",
                $"ZeroQL.GraphQLResponse<{queryType}>",
                $"ZeroQL.GraphQLResponse<{mutationType}>",
                "Dictionary<string, object>",
                "Dictionary<int, string[]>"
            })
            .ToArray();

        var jsonInitializers = options.GenerateJsonInitializers(
            customScalars,
            enums,
            interfaces.Values,
            typesForJsonContext);

        members.Add(new[] { jsonInitializers });
        
        fixedRoot = CreateFile(options, checksum, warningCodes, members);

        var formattedRoot = Formatter.Format(fixedRoot, new AdhocWorkspace());
        var formattedSource = formattedRoot.ToFullString();

        return formattedSource;
    }

    private static SyntaxNode CreateFile(
        GraphQlGeneratorOptions options,
        string checksum,
        string[] warningCodes,
        IEnumerable<IEnumerable<string>> members)
    {
        var namespaceDeclaration = $$"""
                                     // {{checksum}}
                                     // <auto-generated>
                                     // This file generated for ZeroQL {{ZeroQLGenerationInfo.Version}}.
                                     //
                                     // Do not modify this file manually. Any changes will be lost after regeneration.
                                     // <auto-generated/>
                                     #pragma warning disable {{warningCodes.Join(", ")}}
                                     #nullable enable
                                     namespace {{options.ClientNamespace}}
                                     {
                                      using System;
                                      using System.Collections.Generic;
                                      using System.Linq;
                                      using System.Text.Json.Serialization;
                                      using System.Text.Json.Nodes;
                                      using System.Text.Json;
                                      using ZeroQL;
                                      using ZeroQL.Json;
                                      {{string.Join(Environment.NewLine + Environment.NewLine, members.SelectMany(o => o))}}
                                      }
                                     """;


        if (options.NetstandardCompatibility is true)
        {
            namespaceDeclaration += """
                                    // Netstandard compatibility
                                    namespace System.Runtime.CompilerServices
                                    {
                                        [AttributeUsage(AttributeTargets.Method, Inherited = false)]
                                        public sealed class ModuleInitializerAttribute : Attribute
                                        {
                                        }
                                    }
                                    """;
        }

        var root = ParseSyntaxTree(namespaceDeclaration).GetRoot();
        var fixedRoot = FixTypeNamingWhenNameEqualsMemberName(root);
        return fixedRoot;
    }

    private static IEnumerable<string> GetUniqueGraphQLParameters(TypeContext context, SyntaxNode node)
    {
        var defaultJsonTypes = context.DefaultJsonTypes;
        var arrays = defaultJsonTypes.Select(o => $"IEnumerable<{o}>").ToArray();
        
        var graphQLTypeAttributes = node
            .DescendantNodes()
            .OfType<ParameterSyntax>()
            .SelectMany(o => o.AttributeLists)
            .SelectMany(o => o.Attributes)
            .Where(o => o.Name.ToString() == "ZeroQL.GraphQLType")
            .Select(o => o.Parent!.Parent)
            .OfType<ParameterSyntax>()
            .Select(o => o.Type!.ToString())
            .Distinct()
            .Where(o => !o.Contains('?'))
            .Order()
            .ToArray();

        return defaultJsonTypes
            .Concat(arrays)
            .Concat(graphQLTypeAttributes)
            .Distinct()
            .ToArray();
    }

    private static (string? Query, string? Mutation) GetQueryAndMutation(GraphQLDocument document)
    {
        var schemaDefinition = document.Definitions
            .OfType<GraphQLSchemaDefinition>()
            .FirstOrDefault();

        if (schemaDefinition is not null)
        {
            var queryTypeFromSchema = schemaDefinition.OperationTypes
                .FirstOrDefault(x => x.Operation == OperationType.Query)?
                .Type?
                .Name
                .StringValue;

            var mutationTypeFromSchema = schemaDefinition.OperationTypes
                .FirstOrDefault(x => x.Operation == OperationType.Mutation)?
                .Type?
                .Name
                .StringValue;

            return (queryTypeFromSchema, mutationTypeFromSchema);
        }

        var queryType = document.Definitions
            .OfType<GraphQLObjectTypeDefinition>()
            .FirstOrDefault(x => x.Name.StringValue == "Query");

        var mutationType = document.Definitions
            .OfType<GraphQLObjectTypeDefinition>()
            .FirstOrDefault(x => x.Name.StringValue == "Mutation");

        return (queryType?.Name.StringValue, mutationType?.Name.StringValue);
    }

    private static SyntaxNode FixTypeNamingWhenNameEqualsMemberName(SyntaxNode unit)
    {
        var classesWhenClassNameEqualsToPropertyName = unit
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(o => o.Members
                            .OfType<PropertyDeclarationSyntax>()
                            .Any(p => p.Identifier.Text == o.Identifier.Text) ||
                        o.Members
                            .OfType<MethodDeclarationSyntax>()
                            .Any(m => SubstringGenericName(m.Identifier.Text) == o.Identifier.Text))
            .ToArray();

        var changedClasses = classesWhenClassNameEqualsToPropertyName
            .Select(o => (o.Identifier.Text, New: o.WithIdentifier(Identifier($"{o.Identifier.Text}ZeroQL"))))
            .GroupBy(o => o.Text, o => o.New)
            .ToDictionary(o => o.Key, o => o.First());

        unit = unit.ReplaceNodes(classesWhenClassNameEqualsToPropertyName,
            (oldNode, _) => changedClasses[oldNode.Identifier.Text]);

        var propertyTypes = unit
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Select(o => o.Type)
            .ToArray();

        var methodTypes = unit
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Select(o => o.ReturnType)
            .ToArray();

        var methodParameters = unit
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .SelectMany(o => o.ParameterList.Parameters)
            .Select(o => o.Type)
            .ToArray();

        var genericTypes = unit
            .DescendantNodes()
            .OfType<GenericNameSyntax>()
            .Select(o => o.TypeArgumentList.Arguments)
            .SelectMany(o => o)
            .ToArray();

        var types = propertyTypes
            .Concat(methodTypes)
            .Concat(methodParameters)
            .Concat(genericTypes)
            .ToArray();

        var identifies = types
            .SelectMany(GetIdentifiers)
            .Where(o => changedClasses.ContainsKey(o.Identifier.Text))
            .ToArray();

        var changedIdentifiers = identifies
            .Select(o => (Key: o, New: o.WithIdentifier(Identifier($"{o.Identifier.Text}ZeroQL"))))
            .ToDictionary(o => o.Key, o => o.New);

        unit = unit.ReplaceNodes(identifies,
            (oldNode, _) => changedIdentifiers[oldNode]);

        return unit;
    }

    private static IdentifierNameSyntax[] GetIdentifiers(TypeSyntax? typeSyntax)
    {
        return typeSyntax switch
        {
            IdentifierNameSyntax identifierNameSyntax => [identifierNameSyntax],
            ArrayTypeSyntax arrayTypeSyntax => GetIdentifiers(arrayTypeSyntax.ElementType),
            NullableTypeSyntax nullableTypeSyntax => GetIdentifiers(nullableTypeSyntax.ElementType),
            _ => Array.Empty<IdentifierNameSyntax>()
        };
    }

    private static string SubstringGenericName(string name)
    {
        var index = name.IndexOf('<');
        if (index == -1)
        {
            return name;
        }

        return name.Substring(0, index);
    }

    private static ClassDefinition CreateTypesDefinition(
        TypeContext typeContext,
        Dictionary<string, InterfaceDefinition> interfaces,
        GraphQLObjectTypeDefinition type)
    {
        var typeInterfaces = type.Interfaces?
            .Select(o => interfaces[o.Name.StringValue])
            .ToList() ?? new List<InterfaceDefinition>();

        var interfacesFields = typeInterfaces
            .SelectMany(o => o.Properties)
            .ToArray();

        var classFields = new List<FieldDefinition>();
        var classDefinition = new ClassDefinition(type.Name.StringValue, classFields, typeInterfaces);
        var fields = typeContext.CreatePropertyDefinition(classDefinition, type.Fields, interfacesFields);
        classFields.AddRange(fields);

        return classDefinition;
    }

    private static InterfaceDefinition CreateInterfaceDefinition(
        TypeContext typeContext,
        GraphQLInterfaceTypeDefinition definition)
    {
        var names = definition.Interfaces?.Items
            .Select(o => o.Name.StringValue)
            .ToArray() ?? Array.Empty<string>();

        var interfaceFields = new List<FieldDefinition>();
        var interfaceDefinition = new InterfaceDefinition(definition.Name.StringValue, names, interfaceFields);
        var fields = typeContext.CreatePropertyDefinition(
            interfaceDefinition,
            definition.Fields,
            Array.Empty<FieldDefinition>());

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
            var unionInterface = new InterfaceDefinition(
                union.Name,
                Array.Empty<string>(),
                Array.Empty<FieldDefinition>());

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
                            .AddAttributeWithStringParameter(ZeroQLGenerationInfo.GraphQLNameAttribute, o);
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