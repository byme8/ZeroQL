using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ZeroQL.SourceGenerators.Generator;

public record GraphQLQueryGenerationContext(
    string QueryVariableName,
    CSharpSyntaxNode Parent,
    Dictionary<string, string> AvailableVariables,
    SemanticModel SemanticModel,
    CancellationToken CancellationToken)
{
    public string QueryVariableName { get; set; } = QueryVariableName;

    public CSharpSyntaxNode Parent { get; set; } = Parent;

    public Dictionary<string, string> AvailableVariables { get; set; } = AvailableVariables;

    public SemanticModel SemanticModel { get; set; } = SemanticModel;

    public CancellationToken CancellationToken { get; set; } = CancellationToken;

    public GraphQLQueryGenerationContext WithParent(CSharpSyntaxNode parent)
    {
        return this with { Parent = parent };
    }

    public GraphQLQueryGenerationContext WithVariableName(string name)
    {
        return this with { QueryVariableName = name };
    }

}