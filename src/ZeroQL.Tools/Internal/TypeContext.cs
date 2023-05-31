using System;
using System.Collections.Generic;
using GraphQLParser.AST;
using ZeroQL.Bootstrap;
using ZeroQL.Schema;

namespace ZeroQL.Internal;

public class TypeContext
{
    public Dictionary<string, string> GraphQLToCsharpScalarTypes = new()
    {
        { "String", "string" },
        { "Short", "short" },
        { "Byte", "byte" },
        { "Int", "int" },
        { "Long", "long" },
        { "Float", "double" },
        { "Decimal", "decimal" },
        { "DateTime", "DateTimeOffset" },
        { "TimeSpan", "TimeSpan" },
        { "Date", "DateOnly" },
        { "UUID", "Guid" },
        { "ID", "ID" },
        { "Boolean", "bool" },
        { "Upload", "global::ZeroQL.Upload" },
        { "JSON", "global::System.Text.Json.JsonElement" },
    };

    public TypeContext(
        GraphQlGeneratorOptions options,
        HashSet<string> enums,
        string[] customScalarsFromSchema,
        Dictionary<string, string> scalarsToOverride)
    {
        Enums = enums;
        CustomScalars = new List<ScalarDefinition>();
        foreach (var scalar in customScalarsFromSchema)
        {
            if (GraphQLToCsharpScalarTypes.ContainsKey(scalar) || scalarsToOverride.ContainsKey(scalar))
            {
                continue;
            }

            var scalarDefinition = new ScalarDefinition(scalar);
            CustomScalars.Add(scalarDefinition);
            GraphQLToCsharpScalarTypes[scalar] = options.GetDefinitionFullTypeName(scalarDefinition);
        }
        
        foreach (var (key, value) in scalarsToOverride)
        {
            GraphQLToCsharpScalarTypes[key] = options.GetDefinitionFullTypeName(value);
        }
    }

    public List<ScalarDefinition> CustomScalars { get; }

    public HashSet<string> Enums { get; }

    public TypeDefinition GetTypeDefinition(GraphQLType type)
    {
        switch (type)
        {
            case GraphQLNonNullType { Type: GraphQLNamedType nonNullType }:
            {
                var typeDefinition = GetTypeDefinition(nonNullType);
                return typeDefinition;
            }
            case GraphQLNonNullType { Type: GraphQLListType listType }:
            {
                var typeDefinition = GetTypeDefinition(listType.Type);
                return new ListTypeDefinition(typeDefinition.Name + "[]", false, typeDefinition);
            }
            case GraphQLListType listType:
            {
                var typeDefinition = GetTypeDefinition(listType.Type);
                return new ListTypeDefinition(typeDefinition.Name + "[]", true, typeDefinition);
            }
            case GraphQLNamedType namedType:
            {
                var typeDefinition = GetTypeDefinition(namedType);
                return typeDefinition with
                {
                    CanBeNull = true
                };
            }
            default:
                throw new NotSupportedException($"Type '{type}' is not supported");
        }
    }


    private TypeDefinition GetTypeDefinition(GraphQLNamedType namedType)
    {
        if (Enums.Contains(namedType.Name.StringValue))
        {
            return new EnumTypeDefinition(namedType.Name.StringValue);
        }

        return GraphQLToCsharpScalarTypes.ContainsKey(namedType.Name.StringValue)
            ? new ScalarTypeDefinition(GraphQLToCsharpScalarTypes[namedType.Name.StringValue])
            : new ObjectTypeDefinition(namedType.Name.StringValue);
    }
}