using System.Linq;
using Microsoft.CodeAnalysis;

namespace ZeroQL.SourceGenerators.Resolver;

public static class GraphQLConstantResolver
{
    public static string ToGraphQL(ITypeSymbol symbol, object value) =>
        symbol switch
        {
            { SpecialType: SpecialType.System_String } => $@"""{value}""",
            { TypeKind: TypeKind.Enum } => MaterializeEnum(symbol, value),
            _ => value.ToString(),
        };

    private static string MaterializeEnum(ITypeSymbol symbol, object value)
    {
        if (symbol is not INamedTypeSymbol enumType)
        {
            return value.ToString();
        }
        
        var graphQLName = enumType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(o => o.ConstantValue?.Equals(value) ?? false)
            ?.GetAttributes()
            .FirstOrDefault(o => o.AttributeClass?.Name == "GraphQLNameAttribute")
            ?.ConstructorArguments
            .FirstOrDefault()
            .Value?
            .ToString();

        return graphQLName!;
    }
}