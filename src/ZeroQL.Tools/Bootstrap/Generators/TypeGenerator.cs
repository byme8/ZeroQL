using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Extensions;
using ZeroQL.Internal;
using ZeroQL.Schema;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZeroQL.Bootstrap.Generators;

public static class TypeGenerator
{
    public static IEnumerable<MemberDeclarationSyntax> GenerateTypes(
        this GraphQlGeneratorOptions options,
        ClassDefinition[] definitions,
        GraphQLNamedType? queryType,
        GraphQLNamedType? mutationType)
    {
        var csharpDefinitions = definitions
            .Select(o =>
            {
                var @class = CSharpHelper.Class(o.Name, options.Visibility);
                if (o.Implements.Any())
                {
                    var bases = o.Implements
                        .Select(baseType => SimpleBaseType(ParseTypeName(baseType.Name)))
                        .OfType<BaseTypeSyntax>()
                        .ToArray();

                    var interfaceFields = o.Implements
                        .SelectMany(oo => oo.Properties)
                        .ToDictionary(oo => oo.Name);

                    var fields = o.Properties
                        .SelectMany(o => GenerateFieldMembersWithAccountForInterface(o, interfaceFields));

                    @class = @class
                        .AddBaseListTypes(bases)
                        .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                        .WithMembers(List(fields));
                }
                else
                {
                    var fields = o.Properties.SelectMany(GeneratePropertiesDeclarations);
                    @class = @class
                        .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                        .WithMembers(List(fields));
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

    private static IEnumerable<MemberDeclarationSyntax> GenerateFieldMembersWithAccountForInterface(
        FieldDefinition field, Dictionary<string, FieldDefinition> interfaceFields)
    {
        var members = new List<MemberDeclarationSyntax>();
        members.AddRange(GeneratePropertiesDeclarations(field));
        var interfaceField = interfaceFields.GetValueOrDefault(field.Name);
        if (interfaceField is null || interfaceField.TypeDefinition == field.TypeDefinition)
        {
            return members;
        }

        if (members.Count == 2)
        {
            // we have graphql selector and csharp property
            // no need to generate nullable property
            return members;
        }

        var interfaceFieldDeclarations = GeneratePropertiesDeclarations(interfaceField);
        foreach (var interfaceFieldDeclaration in interfaceFieldDeclarations)
        {
            switch (interfaceFieldDeclaration)
            {
                case PropertyDeclarationSyntax property:
                    members.Add(property
                        .WithModifiers(new SyntaxTokenList())
                        .WithAttributeLists(List<AttributeListSyntax>())
                        .AddAttribute(ZeroQLGenerationInfo.JsonIgnoreAttribute)
                        .WithExplicitInterfaceSpecifier(
                            ExplicitInterfaceSpecifier(
                                IdentifierName(interfaceField.Parent.Name))));
                    break;
            }
        }

        return members;
    }

    public static ClassDeclarationSyntax[] GenerateInputs(
        this GraphQlGeneratorOptions options,
        ClassDefinition[] inputs)
    {
        return inputs
            .Select(o =>
            {
                var fields = o.Properties
                    .Select(property =>
                        CSharpHelper.Property(property.Name, property.TypeDefinition, true, property.DefaultValue)
                            .AddAttribute(ZeroQLGenerationInfo.JsonPropertyNameAttribute, property.GraphQLName));

                return CSharpHelper.Class(o.Name, options.Visibility)
                    .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                    .WithMembers(List<MemberDeclarationSyntax>(fields));
            })
            .ToArray();
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

    public static MemberDeclarationSyntax[] GeneratePropertiesDeclarations(this FieldDefinition field)
    {
        if (RequireSelector(field))
        {
            var backedField = BackedField(field);
            var parameters = field.Arguments
                .Select(o =>
                    Parameter(Identifier(o.Name.EnsureNotKeyword()))
                        .WithType(ParseTypeName(o.TypeName)))
                .ToArray();

            var selector = GenerateQueryPropertyDeclaration(field, parameters);
            selector = selector.CopyDirectives(field);

            return new[] { backedField, selector };
        }

        var property = CSharpHelper
            .Property(field.Name, field.TypeDefinition, true, field.DefaultValue)
            .AddAttribute(ZeroQLGenerationInfo.GraphQLFieldSelectorAttribute, field.GraphQLName)
            .AddAttribute(ZeroQLGenerationInfo.JsonPropertyNameAttribute, field.GraphQLName);

        return new[]
        {
            property.CopyDirectives(field)
        };
    }

    private static MemberDeclarationSyntax GenerateQueryPropertyDeclaration(
        FieldDefinition field,
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
                Identifier(name.EnsureNotKeyword()))
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAttribute(ZeroQLGenerationInfo.GraphQLFieldSelectorAttribute, field.GraphQLName)
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

    private static string GetPropertyMethodBody(string fieldName,
        TypeDefinition typeDefinition,
        string? parentFieldName = null)
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
                return
                    $@"{fieldName} is null ? throw new NullReferenceException(""{fieldNameToReport} is null but it should not be null. Schema can be outdated."") : selector({fieldName})";
            case ListTypeDefinition { ElementTypeDefinition: ScalarTypeDefinition or EnumTypeDefinition }:
                return fieldName;
            case ListTypeDefinition { CanBeNull: true } type:
                return
                    $"{fieldName}?.Select(o => {GetPropertyMethodBody("o", type.ElementTypeDefinition, fieldName)}).ToArray()";
            case ListTypeDefinition { CanBeNull: false } type:
                return
                    $@"{fieldName} is null ? throw new NullReferenceException(""{fieldNameToReport} is null but it should not be null. Schema can be outdated."") : {fieldName}.Select(o => {GetPropertyMethodBody("o", type.ElementTypeDefinition, fieldName)}).ToArray()";
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

    private static string? GetDefaultValue(GraphQLInputValueDefinition field)
    {
        if (field.DefaultValue is not IHasValueNode hasValueNode)
            return null;

        return (string)hasValueNode.Value;
    }

    public static FieldDefinition[] CreatePropertyDefinition(
        this TypeContext typeContext,
        Definition parent,
        GraphQLInputFieldsDefinition? fields)
    {
        return fields?
            .Select(field =>
            {
                var graphQLName = field.Name.StringValue;
                var csharpName = graphQLName.FirstToUpper();

                var directives = GetDirectiveDefinitions(field.Directives);

                var type = typeContext.GetTypeDefinition(field.Type);
                var defaultValue = GetDefaultValue(field);
                return new FieldDefinition(
                    csharpName,
                    graphQLName,
                    parent,
                    type,
                    Array.Empty<ArgumentDefinition>(),
                    directives,
                    defaultValue);
            })
            .ToArray() ?? Array.Empty<FieldDefinition>();
    }

    private static DirectiveDefinition[]? GetDirectiveDefinitions(GraphQLDirectives? graphQLDirectives)
    {
        var directives = graphQLDirectives?
            .Select(directive => new DirectiveDefinition(
                directive.Name.StringValue,
                directive.Arguments?
                    .ToDictionary(
                        argument => argument.Name.StringValue,
                        GetArgumentValue)))
            .ToArray();

        return directives;

        static string? GetArgumentValue(GraphQLArgument argument) => argument.Value switch
        {
            GraphQLStringValue value => value.Value.ToString(),
            _ => null
        };
    }


    public static FieldDefinition[] CreatePropertyDefinition(
        this TypeContext typeContext,
        Definition parent,
        GraphQLFieldsDefinition? fields)
    {
        return fields?.Select(field =>
            {
                var type = typeContext.GetTypeDefinition(field.Type);
                var graphQLName = field.Name.StringValue;
                var csharpName = graphQLName.FirstToUpper();

                var argumentDefinitions = field.Arguments?
                    .Select(arg =>
                        new ArgumentDefinition(
                            arg.Name.StringValue,
                            typeContext.GetTypeDefinition(arg.Type)
                                .NameWithNullableAnnotation()))
                    .ToArray() ?? Array.Empty<ArgumentDefinition>();

                var directives = GetDirectiveDefinitions(field.Directives);

                return new FieldDefinition(
                    csharpName,
                    graphQLName,
                    parent,
                    type,
                    argumentDefinitions,
                    directives,
                    null);
            })
            .ToArray() ?? Array.Empty<FieldDefinition>();
    }
}