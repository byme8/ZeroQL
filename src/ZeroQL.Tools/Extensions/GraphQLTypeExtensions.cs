using System;
using GraphQLParser.AST;

namespace ZeroQL.Extensions;

public static class GraphQLTypeExtensions
{
    public static string ToFullString(this GraphQLType type)
    {
        switch (type)
        {
            case GraphQLNonNullType { Type: GraphQLNamedType nonNullType }:
            {
                var typeDefinition = ToFullString(nonNullType);
                return $"{typeDefinition}!";
            }
            case GraphQLNonNullType { Type: GraphQLListType listType }:
            {
                var typeDefinition = ToFullString(listType.Type);
                return $"[{typeDefinition}]!";
            }
            case GraphQLListType listType:
            {
                var typeDefinition = ToFullString(listType.Type);
                return $"[{typeDefinition}]";
            }
            case GraphQLNamedType namedType:
            {
                var typeDefinition = ToFullString(namedType);
                return typeDefinition;
            }
            default:
                throw new NotSupportedException($"Type '{type}' is not supported");
        }
    }

    private static string ToFullString(GraphQLNamedType nonNullType)
    {
        return nonNullType.Name.StringValue;
    }
}