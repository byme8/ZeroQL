using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace ZeroQL.SourceGenerators.Generator;

public class GraphQLQueryGenerationContext
{
    public GraphQLQueryGenerationContext(string queryVariableName, Dictionary<string, string> availableVariables, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        QueryVariableName = queryVariableName;
        AvailableVariables = availableVariables;
        SemanticModel = semanticModel;
        CancellationToken = cancellationToken;
    }

    public string QueryVariableName { get; }

    public Dictionary<string, string> AvailableVariables { get; }

    public SemanticModel SemanticModel { get; }

    public CancellationToken CancellationToken
    {
        get;
    }
}