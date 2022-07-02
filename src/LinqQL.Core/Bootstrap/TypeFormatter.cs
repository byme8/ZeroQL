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
        { "Int", "int" },
        { "Long", "long" },
        { "Float", "float" },
        { "Double", "double" },
        { "Date", "DateTime" },
        { "UUID", "Guid" },
        { "Boolean", "bool" },
        { "DateTime", "DateTime" }
    };

    public TypeFormatter(HashSet<string> enums)
    {
        Enums = enums;
    }

    public HashSet<string> Enums
    {
        get;
    }

    public TypeDefinition GetTypeDefinition(GraphQLType type)
    {
        switch (type)
        {
            case GraphQLNonNullType { Type: GraphQLNamedType nonNullType }:
            {
                var fieldKind = GetTypeKind(nonNullType);
                var typeName = GetTypeName(nonNullType);

                return new TypeDefinition
                {
                    Name = typeName,
                    TypeKind = fieldKind
                };
            }
            case GraphQLNonNullType { Type: GraphQLListType listType }:
            {
                var elementType = GetTypeDefinition(listType.Type);
                var fieldKind = new List(elementType.TypeKind);
                var typeName = elementType.Name + "[]";

                return new TypeDefinition
                {
                    Name = typeName,
                    TypeKind = fieldKind
                };
            }
            case GraphQLListType listType:
            {
                var elementType = GetTypeDefinition(listType.Type);
                var fieldKind = new List(elementType.TypeKind);
                var typeName = elementType.Name + "[]?";

                return new TypeDefinition
                {
                    Name = typeName,
                    TypeKind = fieldKind
                };
            }
            case GraphQLNamedType namedType:
            {
                var fieldKind = GetTypeKind(namedType);
                var typeName = GetTypeName(namedType);

                return new TypeDefinition
                {
                    Name = typeName + "?",
                    TypeKind = fieldKind
                };
            }
            default:
                return new TypeDefinition();
        }
    }


    private string GetTypeName(GraphQLNamedType nonNullType)
    {
        var fieldKind = GetTypeKind(nonNullType);
        var typeName = nonNullType.Name.StringValue;
        return fieldKind == TypeKind.Scalar ? GraphQLToCsharpScalarTypes[typeName] : typeName;
    }

    private TypeKind GetTypeKind(GraphQLNamedType namedType)
    {
        if (Enums.Contains(namedType.Name.StringValue))
        {
            return TypeKind.Enum;
        }
        return GraphQLToCsharpScalarTypes.ContainsKey(namedType.Name.StringValue) ? TypeKind.Scalar : TypeKind.Complex;
    }
}