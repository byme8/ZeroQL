using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ZeroQL.SourceGenerators.Generator;

public record struct GraphQLQueryGenerationContext(
    string QueryVariableName,
    CSharpSyntaxNode Parent,
    Dictionary<string, string> AvailableVariables,
    SemanticModel SemanticModel,
    INamedTypeSymbol FieldSelectorAttribute,
    INamedTypeSymbol FragmentAttribute,
    CancellationToken CancellationToken)
{
    public GraphQLQueryGenerationContext WithParent(CSharpSyntaxNode parent)
    {
        return this with { Parent = parent };
    }

    public GraphQLQueryGenerationContext WithVariableName(string name)
    {
        return this with { QueryVariableName = name };
    }

}