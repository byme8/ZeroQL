using Microsoft.CodeAnalysis;

namespace ZeroQL.SourceGenerators.Resolver;

public class GraphQLQueryVariable
{
    public static GraphQLQueryVariable Variable(string name, ITypeSymbol? typeSymbol = null)
    {
        var variable = new GraphQLQueryVariable
        {
            Name = name,
            GraphQLValue = $"${name.FirstToLower()}",
            TypeSymbol = typeSymbol,
            GraphQLType = typeSymbol?.ToGraphQLType(),
        };

        return variable;
    }
    
    public static GraphQLQueryVariable Constant(string value)
    {
        var variable = new GraphQLQueryVariable
        {
            GraphQLValue = value,
        };

        return variable;
    }

    public string Name { get; private set; }

    public ITypeSymbol? TypeSymbol { get; private set; }

    public string GraphQLValue { get; private set; }

    public string? GraphQLType { get; private set; }
}