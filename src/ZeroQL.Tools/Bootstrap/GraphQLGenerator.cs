using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphQLParser;
using GraphQLParser.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        var scalarTypes = schema.Definitions
            .OfType<GraphQLScalarTypeDefinition>()
            .Select(o => o.Name.StringValue)
            .ToArray();

        var context = new TypeContext(options, enumsNames, scalarTypes);
        var customScalars = context.CustomScalars;

        var inputs = schema.Definitions
            .OfType<GraphQLInputObjectTypeDefinition>()
            .Select(o => CreateInputDefinition(context, o))
            .ToArray();

        var types = schema.Definitions
            .OfType<GraphQLObjectTypeDefinition>()
            .Select(o => CreateTypesDefinition(context, o))
            .ToArray();

        var interfaces = schema.Definitions
            .OfType<GraphQLInterfaceTypeDefinition>()
            .Select(o => CreateInterfaceDefinition(context, o))
            .ToList();

        AddUnions(schema, interfaces, types);

        var namespaceDeclaration = NamespaceDeclaration(IdentifierName(options.ClientNamespace));
        var clientDeclaration = new[] { GenerateClient(options, queryType, mutationType) };
        var typesDeclaration = GenerateTypes(options, types, queryType, mutationType);
        var interfacesDeclaration = GenerateInterfaces(options, interfaces);
        var inputsDeclaration = GenerateInputs(options, inputs);
        var enumsDeclaration = GenerateEnums(options, enums);
        var scalarDeclaration = GenerateScalars(options, customScalars);
        var jsonInitializers = GenerateJsonInitializers(options, customScalars, enums, interfaces);
        var interfaceInitializers = GenerateInterfaceInitializers(interfaces, types);

        namespaceDeclaration = namespaceDeclaration
            .WithMembers(List<MemberDeclarationSyntax>(clientDeclaration)
                .AddRange(typesDeclaration)
                .AddRange(interfacesDeclaration)
                .AddRange(inputsDeclaration)
                .AddRange(scalarDeclaration)
                .AddRange(enumsDeclaration)
                .AddRange(interfaceInitializers)
                .Add(jsonInitializers));

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

        var formattedSource = namespaceDeclaration
            .NormalizeWhitespace()
            .ToFullString();

        return formattedSource;
    }

    private static RecordDeclarationSyntax[] GenerateScalars(GraphQlGeneratorOptions options,
        IReadOnlyList<ScalarDefinition> customScalars)
    {
        return customScalars
            .Select(scalarDefinition =>
            {
                var source = $$"""
                    {{options.AccessLevel}} sealed record {{scalarDefinition.Name}} : ZeroQLScalar
                    {
                        public {{scalarDefinition.Name}}()
                        {
                        }

                        public {{scalarDefinition.Name}}(string value)
                        {
                            Value = value;
                        }

                        public static implicit operator {{scalarDefinition.Name}}(string value) => new {{scalarDefinition.Name}}(value);

                        public static implicit operator string({{scalarDefinition.Name}} scalar) => scalar.Value;
                    }
                    """;

                var classDeclarationSyntax = ParseCompilationUnit(source)
                    .DescendantNodesAndSelf()
                    .OfType<RecordDeclarationSyntax>()
                    .First();

                return classDeclarationSyntax;
            })
            .ToArray();
    }

    private static void AddUnions(GraphQLDocument schema, List<InterfaceDefinition> interfaces, ClassDefinition[] types)
    {
        var unions = schema.Definitions
            .OfType<GraphQLUnionTypeDefinition>()
            .Select(CreateUnionDefinition)
            .ToArray();

        foreach (var union in unions)
        {
            interfaces.Add(new InterfaceDefinition(union.Name, Array.Empty<FieldDefinition>()));
            foreach (var unionType in union.Types)
            {
                var type = types.FirstOrDefault(o => o.Name == unionType);
                if (type is not null)
                {
                    type.Implements.Add(union.Name);
                }
            }
        }
    }

    private static ClassDeclarationSyntax[] GenerateInterfaceInitializers(
        List<InterfaceDefinition> interfaces,
        ClassDefinition[] types)
    {
        var typesByInterface = types
            .Where(o => o.Implements.Any())
            .SelectMany(o => o.Implements.Select(oo => (Interface: oo, Type: o)))
            .GroupBy(o => o.Interface)
            .ToDictionary(o => o.Key, o => o.ToArray());

        foreach (var @interface in interfaces)
        {
            if (typesByInterface.ContainsKey(@interface.Name))
            {
                continue;
            }

            typesByInterface.Add(@interface.Name, Array.Empty<(string Interface, ClassDefinition Type)>());
        }

        if (!typesByInterface.Any())
        {
            return Array.Empty<ClassDeclarationSyntax>();
        }

        var classes = typesByInterface.Select(group =>
            {
                var typeName = group.Key;
                var source = $$"""
                    internal class ZeroQL{{typeName}}Converter : InterfaceJsonConverter<{{typeName}}?>
                    {
                        public override {{typeName}}? Deserialize(string typeName, JsonObject json) =>
                            typeName switch
                            {
                                {{group.Value
                                    .Select(o => $@"""{o.Type.Name}"" => json.Deserialize<{o.Type.Name}>(ZeroQLJsonOptions.Options),")
                                    .JoinWithNewLine()}}
                                _ => json.Deserialize<{{typeName}}Stub>(ZeroQLJsonOptions.Options)
                            };
                    }

                    """;

                return source;
            })
            .JoinWithNewLine();

        var syntaxTree = ParseSyntaxTree(classes)
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .ToArray();

        return syntaxTree;
    }

    private static ClassDeclarationSyntax GenerateClient(GraphQlGeneratorOptions options, GraphQLNamedType? queryType,
        GraphQLNamedType? mutationType)
    {
        var clientName = options.ClientName;
        var queryTypeName = queryType?.Name.StringValue ?? "ZeroQL.Unit";
        var mutationTypeName = mutationType?.Name.StringValue ?? "ZeroQL.Unit";

        return CSharpHelper.Class(clientName ?? "GraphQLClient", options.Visibility)
            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(IdentifierName($"global::ZeroQL.GraphQLClient<{queryTypeName}, {mutationTypeName}>")))))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(
                ConstructorDeclaration(clientName ?? "GraphQLClient")
                    .WithParameterList(ParseParameterList(
                        "(global::System.Net.Http.HttpClient client, global::ZeroQL.Pipelines.IGraphQLQueryPipeline? queryPipeline = null)"))
                    // call base constructor
                    .WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(SeparatedList<ArgumentSyntax>()
                            .Add(Argument(IdentifierName("client")))
                            .Add(Argument(IdentifierName("queryPipeline")))
                        )))
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithBody(Block())));
    }

    private static ClassDeclarationSyntax[] GenerateInputs(
        GraphQlGeneratorOptions options,
        ClassDefinition[] inputs)
    {
        return inputs
            .Select(o =>
            {
                var fields = o.Properties
                    .Select(property =>
                        CSharpHelper.Property(property.Name, property.TypeDefinition, true, property.DefaultValue));

                return CSharpHelper.Class(o.Name, options.Visibility)
                    .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                    .WithMembers(List<MemberDeclarationSyntax>(fields));
            })
            .ToArray();
    }

    private static ClassDefinition CreateInputDefinition(TypeContext typeContext,
        GraphQLInputObjectTypeDefinition input)
        => new(input.Name.StringValue, CreatePropertyDefinition(typeContext, input), new List<string>());

    private static EnumDeclarationSyntax[] GenerateEnums(
        GraphQlGeneratorOptions options,
        EnumDefinition[] enums)
    {
        return enums.Select(e =>
            {
                var members = e.Values?.Select(o =>
                    {
                        var name = o.ToPascalCase();
                        return EnumMemberDeclaration(Identifier(name));
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

    private static ClassDeclarationSyntax GenerateJsonInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyList<ScalarDefinition> customScalars,
        IReadOnlyList<EnumDefinition> enums,
        IReadOnlyList<InterfaceDefinition> interfaces)
    {
        var customScalarInitializers = CustomScalarInitializers(options, customScalars);
        var enumInitializers = EnumInitializers(options, enums);
        var interfaceInitializers = InterfaceInitializers(options, interfaces);

        var source = @$"
            internal static class JsonConvertersInitializers
            {{
                [global::System.Runtime.CompilerServices.ModuleInitializer]
                public static void Init()
                {{
                    {customScalarInitializers}
                    {enumInitializers}
                    {interfaceInitializers}
                }} 
            }}
            ";

        var classDeclarationSyntax = ParseSyntaxTree(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        return classDeclarationSyntax;
    }

    private static StringBuilder InterfaceInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyList<InterfaceDefinition> interfaces)
    {
        var sb = new StringBuilder();
        foreach (var interfaceDefinition in interfaces)
        {
            var typeName = options.GetDefinitionFullTypeName(interfaceDefinition);
            sb.AppendLine(
                $"global::ZeroQL.Json.ZeroQLJsonSerializersStore.Converters[typeof({typeName})] = new ZeroQL{interfaceDefinition.Name}Converter();");
        }

        return sb;
    }

    private static StringBuilder CustomScalarInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyList<ScalarDefinition> customScalars)
    {
        var sb = new StringBuilder();
        foreach (var scalar in customScalars)
        {
            var fullTypeName = options.GetDefinitionFullTypeName(scalar);
            sb.AppendLine(
                $"global::ZeroQL.Json.ZeroQLJsonSerializersStore.Converters[typeof({fullTypeName})] = new ZeroQLScalarJsonConverter<{fullTypeName}>();");
        }

        return sb;
    }

    private static StringBuilder EnumInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyList<EnumDefinition> enums)
    {
        var enumInitializers = new StringBuilder();
        foreach (var @enum in enums)
        {
            var enumName = options.GetDefinitionFullTypeName(@enum);
            enumInitializers.AppendLine(
                @$"global::ZeroQL.Json.ZeroQLJsonSerializersStore.Converters[typeof({enumName})] =");
            enumInitializers.AppendLine(@$"
                new global::ZeroQL.Json.ZeroQLEnumConverter<{enumName}>(
                    new global::System.Collections.Generic.Dictionary<string, {enumName}>
                    {{");

            if (@enum.Values is not null)
            {
                foreach (var value in @enum.Values)
                {
                    enumInitializers.AppendLine(
                        @$"{{ ""{value}"", {enumName}.{value.ToPascalCase()} }}, ");
                }
            }

            enumInitializers.AppendLine(@$"
                }},
                new global::System.Collections.Generic.Dictionary<{enumName}, string>
                {{");

            if (@enum.Values is not null)
            {
                foreach (var value in @enum.Values)
                {
                    enumInitializers.AppendLine(
                        @$"{{ {enumName}.{value.ToPascalCase()}, ""{value}"" }},");
                }
            }

            enumInitializers.AppendLine("});");
        }

        return enumInitializers;
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateTypes(
        GraphQlGeneratorOptions options,
        ClassDefinition[] definitions,
        GraphQLNamedType? queryType, GraphQLNamedType? mutationType)
    {
        var csharpDefinitions = definitions
            .Select(o =>
            {
                var fields = o.Properties.SelectMany(GeneratePropertiesDeclarations);
                var @class = CSharpHelper.Class(o.Name, options.Visibility)
                    .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                    .WithMembers(List(fields));

                if (o.Implements.Any())
                {
                    var bases = o.Implements
                        .Select(baseType => SimpleBaseType(ParseTypeName(baseType)))
                        .OfType<BaseTypeSyntax>()
                        .ToArray();

                    @class = @class
                        .AddBaseListTypes(bases);
                }

                if (o.Name == queryType?.Name.StringValue)
                {
                    @class = @class.AddBaseListTypes(SimpleBaseType(IdentifierName("global::ZeroQL.Internal.IQuery")));
                }

                if (o.Name == mutationType?.Name.StringValue)
                {
                    @class = @class.AddBaseListTypes(
                        SimpleBaseType(IdentifierName("global::ZeroQL.Internal.IMutation")));
                }

                return @class;
            })
            .ToList();

        return csharpDefinitions;
    }

    private static PropertyDeclarationSyntax BackedField(FieldDefinition field)
    {
        var jsonNameAttributes = new[]
        {
            ("global::System.ComponentModel.EditorBrowsable",
                "global::System.ComponentModel.EditorBrowsableState.Never"),
            ("JsonPropertyName", Literal(field.Name).Text)
        };

        return CSharpHelper
            .Property("__" + field.Name, field.TypeDefinition, false, null)
            .AddAttributes(jsonNameAttributes);
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateInterfaces(
        GraphQlGeneratorOptions options,
        IReadOnlyCollection<InterfaceDefinition> interfaces)
    {
        var csharpDefinitions = interfaces
            .SelectMany(o =>
            {
                var fields = o.Properties
                    .SelectMany(GeneratePropertiesDeclarations)
                    .ToArray();

                var @interface = CSharpHelper.Interface(o.Name, options.Visibility)
                    .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                    .AddBaseListTypes(SimpleBaseType(ParseTypeName("global::ZeroQL.IUnionType")))
                    .WithMembers(List(fields));

                var stub = CSharpHelper.Class(o.Name + "Stub", options.Visibility)
                    .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                    .AddBaseListTypes(SimpleBaseType(ParseTypeName(o.Name)))
                    .WithMembers(List(fields));

                return new MemberDeclarationSyntax[] { @interface, stub };
            })
            .ToList();

        return csharpDefinitions;
    }

    private static MemberDeclarationSyntax[] GeneratePropertiesDeclarations(FieldDefinition field)
    {
        if (RequireSelector(field))
        {
            var backedField = BackedField(field);

            var parameters = field.Arguments
                .Select(o =>
                    Parameter(Identifier(o.Name))
                        .WithType(ParseTypeName(o.TypeName)))
                .ToArray();

            var selector = GenerateQueryPropertyDeclaration(field, parameters);
            return new[] { backedField, selector };
        }

        return new MemberDeclarationSyntax[]
        {
            CSharpHelper.Property(field.Name, field.TypeDefinition, true, field.DefaultValue)
        };
    }


    private static MemberDeclarationSyntax GenerateQueryPropertyDeclaration(FieldDefinition field,
        ParameterSyntax[] parameters)
    {
        var returnType = GetPropertyReturnType(field.TypeDefinition);
        var name = GetPropertyName(field.Name, field.TypeDefinition);
        var methodBody = $"return {GetPropertyMethodBody("__" + field.Name, field.TypeDefinition)};";

        var funcType = GetPropertyFuncType(field.TypeDefinition, true);
        var selectorParameter = Parameter(Identifier("selector")).WithType(ParseTypeName($"Func<{funcType}, T>"));

        var list = SeparatedList(parameters);
        if (RequireSelector(field.TypeDefinition))
        {
            list = list.Add(selectorParameter);
        }

        var genericMethodWithType = MethodDeclaration(
                IdentifierName(returnType),
                Identifier(name))
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(AttributeList()
                .AddAttributes(
                    Attribute(
                        ParseName(ZeroQLGenerationInfo.GraphQLFieldSelectorAttribute))))
            .WithParameterList(ParameterList(list));

        var body = Block(
            ParseStatement(methodBody));

        return genericMethodWithType
            .WithBody(body);
    }

    private static bool RequireSelector(TypeDefinition typeDefinition)
    {
        switch (typeDefinition)
        {
            case ObjectTypeDefinition:
                return true;
            case ScalarTypeDefinition:
            case EnumTypeDefinition:
                return false;
            case ListTypeDefinition type:
                return RequireSelector(type.ElementTypeDefinition);
            default:
                throw new NotImplementedException();
        }
    }

    private static bool RequireSelector(FieldDefinition field)
    {
        if (field.Arguments.Any())
        {
            return true;
        }

        switch (field.TypeDefinition)
        {
            case ObjectTypeDefinition:
                return true;
            case ScalarTypeDefinition:
            case EnumTypeDefinition:
                return false;
            case ListTypeDefinition type:
                return RequireSelector(type.ElementTypeDefinition);
            default:
                throw new NotImplementedException();
        }
    }

    private static string GetPropertyName(string fieldName, TypeDefinition typeDefinition)
    {
        switch (typeDefinition)
        {
            case ObjectTypeDefinition:
                return fieldName + "<T>";
            case ScalarTypeDefinition:
            case EnumTypeDefinition:
                return fieldName;
            case ListTypeDefinition type:
                return GetPropertyName(fieldName, type.ElementTypeDefinition);
            default:
                throw new NotImplementedException();
        }
    }

    private static string GetPropertyFuncType(TypeDefinition typeDefinition, bool ignoreNullable)
    {
        switch (typeDefinition)
        {
            case ObjectTypeDefinition:
            case ScalarTypeDefinition:
            case EnumTypeDefinition:
                var typeName = typeDefinition.Name;
                if (ignoreNullable)
                {
                    return typeName;
                }

                return typeDefinition.Name + typeDefinition.NullableAnnotation();
            case ListTypeDefinition type:
                return GetPropertyFuncType(type.ElementTypeDefinition, ignoreNullable);
            default:
                throw new NotImplementedException();
        }
    }

    private static string GetPropertyMethodBody(string fieldName, TypeDefinition typeDefinition, string? parentFieldName = null)
    {
        var fieldNameToReport = (parentFieldName ?? fieldName).TrimStart('_');
        switch (typeDefinition)
        {
            case ScalarTypeDefinition:
            case EnumTypeDefinition:
                return fieldName;
            case ObjectTypeDefinition { CanBeNull: true }:
                return $"{fieldName} is null ? default : selector({fieldName})";
            case ObjectTypeDefinition { CanBeNull: false }:
                return $@"{fieldName} is null ? throw new NullReferenceException(""{fieldNameToReport} is null but it should not be null. Schema can be outdated."") : selector({fieldName})";
            case ListTypeDefinition { ElementTypeDefinition: ScalarTypeDefinition or EnumTypeDefinition }:
                return fieldName;
            case ListTypeDefinition { CanBeNull: true } type:
                return $"{fieldName}?.Select(o => {GetPropertyMethodBody("o", type.ElementTypeDefinition, fieldName)}).ToArray()";
            case ListTypeDefinition { CanBeNull: false } type:
                return $@"{fieldName} is null ? throw new NullReferenceException(""{fieldNameToReport} is null but it should not be null. Schema can be outdated."") : {fieldName}.Select(o => {GetPropertyMethodBody("o", type.ElementTypeDefinition, fieldName)}).ToArray()";
            default:
                throw new NotImplementedException();
        }
    }

    private static string GetPropertyReturnType(TypeDefinition typeDefinition)
    {
        switch (typeDefinition)
        {
            case ObjectTypeDefinition type:
                return "T" + type.NullableAnnotation();
            case ScalarTypeDefinition type:
                return type.NameWithNullableAnnotation();
            case EnumTypeDefinition type:
                return type.NameWithNullableAnnotation();
            case ListTypeDefinition type:
                return $"{GetPropertyReturnType(type.ElementTypeDefinition)}[]{type.NullableAnnotation()}";
            default:
                throw new NotImplementedException();
        }
    }

    private static ClassDefinition CreateTypesDefinition(TypeContext typeContext, GraphQLObjectTypeDefinition type)
        => new(type.Name.StringValue, CreatePropertyDefinition(typeContext, type.Fields),
            type.Interfaces?.Select(o => o.Name.StringValue).ToList() ?? new List<string>());

    private static InterfaceDefinition CreateInterfaceDefinition(TypeContext typeContext,
        GraphQLInterfaceTypeDefinition definition)
        => new(definition.Name.StringValue, CreatePropertyDefinition(typeContext, definition.Fields));

    private static UnionDefinition CreateUnionDefinition(GraphQLUnionTypeDefinition union)
    {
        return new UnionDefinition(
            union.Name.StringValue,
            union.Types?.Select(o => o.Name.StringValue).ToArray() ?? Array.Empty<string>());
    }

    private static string? GetDefaultValue(GraphQLInputValueDefinition field)
    {
        if (field.DefaultValue is not IHasValueNode hasValueNode)
            return null;

        return (string)hasValueNode.Value;
    }


    private static FieldDefinition[] CreatePropertyDefinition(TypeContext typeContext,
        GraphQLInputObjectTypeDefinition typeQL)
    {
        return typeQL.Fields?
            .Select(field =>
            {
                var type = typeContext.GetTypeDefinition(field.Type);
                var defaultValue = GetDefaultValue(field);
                return new FieldDefinition(field.Name.StringValue.FirstToUpper(), type,
                    Array.Empty<ArgumentDefinition>(), defaultValue);
            })
            .ToArray() ?? Array.Empty<FieldDefinition>();
    }

    private static FieldDefinition[] CreatePropertyDefinition(TypeContext typeContext,
        GraphQLFieldsDefinition? fields)
    {
        return fields?.Select(field =>
            {
                var type = typeContext.GetTypeDefinition(field.Type);
                return new FieldDefinition(
                    field.Name.StringValue.FirstToUpper(),
                    type,
                    field.Arguments?
                        .Select(arg => new ArgumentDefinition(arg.Name.StringValue,
                            typeContext.GetTypeDefinition(arg.Type).NameWithNullableAnnotation()))
                        .ToArray() ?? Array.Empty<ArgumentDefinition>(),
                    null);
            })
            .ToArray() ?? Array.Empty<FieldDefinition>();
    }
}