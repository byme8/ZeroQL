using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ZeroQL.SourceGenerators.Resolver;

public record struct GraphQLResolveContext(
    string QueryVariableName,
    CSharpSyntaxNode Parent,
    Dictionary<string, GraphQLQueryVariable> AvailableVariables,
    SemanticModel SemanticModel,
    CancellationToken CancellationToken)
{
    private INamedTypeSymbol? unionType;
    private INamedTypeSymbol? syntaxAttribute;
    private INamedTypeSymbol? fragmentAttribute = null;
    private INamedTypeSymbol? graphQLNameAttribute = null;
    private INamedTypeSymbol? graphQLTypeAttribute = null;
    private INamedTypeSymbol? fragmentQueryAttribute = null;
    private SemanticModel semanticModel = SemanticModel;

    public INamedTypeSymbol FragmentAttribute
    {
        get => fragmentAttribute ??=
            SemanticModel.Compilation.GetTypeByMetadataName(SourceGeneratorInfo.GraphQLFragmentAttributeFullName)!;
    }

    public INamedTypeSymbol TemplateAttribute
    {
        get => fragmentQueryAttribute ??=
            SemanticModel.Compilation.GetTypeByMetadataName(SourceGeneratorInfo.GraphQLQueryTemplateAttribute)!;
    }

    public INamedTypeSymbol GraphQLNameAttribute
    {
        get => graphQLNameAttribute ??=
            SemanticModel.Compilation.GetTypeByMetadataName(SourceGeneratorInfo.GraphQLNameAttribute)!;
    }

    public INamedTypeSymbol GraphQLTypeAttribute
    {
        get => graphQLTypeAttribute ??=
            SemanticModel.Compilation.GetTypeByMetadataName(SourceGeneratorInfo.GraphQLNameAttribute)!;
    }

    public INamedTypeSymbol SyntaxAttribute
    {
        get => syntaxAttribute ??=
            SemanticModel.Compilation.GetTypeByMetadataName(SourceGeneratorInfo.GraphQLSyntaxAttribute)!;
    }

    public INamedTypeSymbol UnionType
    {
        get => unionType ??= SemanticModel.Compilation.GetTypeByMetadataName(SourceGeneratorInfo.GraphQLUnionType)!;
    }

    public SemanticModel SemanticModel
    {
        readonly get => semanticModel;
        set
        {
            semanticModel = value;
            unionType = null;
            syntaxAttribute = null;
            fragmentAttribute = null;
            graphQLNameAttribute = null;
            graphQLTypeAttribute = null;
            fragmentQueryAttribute = null;
        }
    }

    public GraphQLResolveContext WithParent(CSharpSyntaxNode parent)
    {
        return this with { Parent = parent };
    }

    public GraphQLResolveContext WithVariableName(string name)
    {
        return this with { QueryVariableName = name };
    }
}