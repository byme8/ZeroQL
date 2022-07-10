using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ZeroQL.SourceGenerators.Generator
{
    public class GraphQLQueryGenerationContext
    {
        public GraphQLQueryGenerationContext(string queryVariableName, Dictionary<string, string> availableVariables, SemanticModel semanticModel)
        {
            QueryVariableName = queryVariableName;
            AvailableVariables = availableVariables;
            SemanticModel = semanticModel;
        }

        public string QueryVariableName { get; }

        public Dictionary<string, string> AvailableVariables { get; }

        public SemanticModel SemanticModel { get; }
    }
}