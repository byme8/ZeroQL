using System.Collections.Generic;
using GraphQLParser.AST;
using LinqQL.Core.Schema;

namespace LinqQL.Core.Bootstrap
{
    public class TypeFormatter
    {
        public Dictionary<GraphQLType, TypeDefinition> Cache = new();

        public Dictionary<string, string> GraphQLToCsharpScalarTypes = new()
        {
            { "String", "string" },
            { "Int", "int" }
        };

        public TypeDefinition GetTypeDefinition(GraphQLType type)
        {
            var cacheValue = Cache.GetValueOrDefault(type);
            if (cacheValue is not null)
            {
                return cacheValue;
            }

            switch (type)
            {
                case GraphQLNonNullType { Type: GraphQLNamedType nonNullType }:
                {
                    var fieldKind = GetScalarity(nonNullType);
                    var typeName = GetTypeName(nonNullType);

                    return new TypeDefinition
                    {
                        Name = typeName,
                        TypeKind = fieldKind,
                    };
                }
                case GraphQLNonNullType { Type: GraphQLListType listType }:
                {
                    var elementType = GetTypeDefinition(listType.Type);
                    var fieldKind = TypeKind.Object;
                    var typeName = elementType.Name + "[]";

                    return new TypeDefinition
                    {
                        Name = typeName,
                        TypeKind = fieldKind,
                    };
                }
                case GraphQLNamedType namedType:
                {
                    var fieldKind = GetScalarity(namedType);
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
            var fieldKind = GetScalarity(nonNullType);
            var typeName = nonNullType.Name.StringValue;
            return fieldKind == TypeKind.Object ? typeName : GraphQLToCsharpScalarTypes[typeName];
        }

        private TypeKind GetScalarity(GraphQLNamedType namedType)
            => GraphQLToCsharpScalarTypes.ContainsKey(namedType.Name.StringValue) ? TypeKind.Scalar : TypeKind.Object;
    }
}