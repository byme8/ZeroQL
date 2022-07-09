using System;
using System.Collections.Generic;
using GraphQLParser.AST;
using LinqQL.Core.Schema;

namespace LinqQL.Core.Bootstrap;

public class TypeFormatter
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
        { "Date", "DateOnly" },
        { "UUID", "Guid" },
        { "Boolean", "bool" },
    };

    public TypeFormatter(HashSet<string> enums)
    {
        Enums = enums;
    }

    public HashSet<string> Enums
    {
        get;
    }

    public TypeDefinition GetTypeDefinition(GraphQLParser.AST.GraphQLType type)
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
                return new ListTypeDefinition
                {
                    Name = typeDefinition.Name + "[]",
                    CanBeNull = false,
                    ElementTypeDefinition = typeDefinition
                };
            }
            case GraphQLListType listType:
            {
                var typeDefinition = GetTypeDefinition(listType.Type);
                return new ListTypeDefinition
                {
                    Name = typeDefinition.Name + "[]",
                    CanBeNull = true,
                    ElementTypeDefinition = typeDefinition
                };
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