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
    CancellationToken CancellationToken)
{
    private INamedTypeSymbol? fragmentAttribute = null;
    private INamedTypeSymbol? fieldSelectorAttribute = null;
    private SemanticModel semanticModel = SemanticModel;

    public INamedTypeSymbol FragmentAttribute
    {
        get => fragmentAttribute ??= SemanticModel.Compilation.GetTypeByMetadataName(SourceGeneratorInfo.GraphQLFragmentAttribute)!;
    }

    public INamedTypeSymbol FieldSelectorAttribute
    {
        get => fieldSelectorAttribute ??= SemanticModel.Compilation.GetTypeByMetadataName(SourceGeneratorInfo.GraphQLFieldSelectorAttribute)!;
    }

    public SemanticModel SemanticModel
    {
        readonly get => semanticModel;
        set
        {
            semanticModel = value;
            fieldSelectorAttribute = null;
            fragmentAttribute = null;
        }
    }

    public GraphQLQueryGenerationContext WithParent(CSharpSyntaxNode parent)
    {
        return this with { Parent = parent };
    }

    public GraphQLQueryGenerationContext WithVariableName(string name)
    {
        return this with { QueryVariableName = name };
    }

}