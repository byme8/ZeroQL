using Microsoft.CodeAnalysis;

namespace LinqQL.SourceGenerators
{
    public class Descriptors
    {
        public static DiagnosticDescriptor FailedToConvert = new DiagnosticDescriptor(
            nameof(FailedToConvert),
            "Failed to convert to graphql query",
            "Failed to convert to graphql query: {0}",
            "LinqQL",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        
        public static DiagnosticDescriptor DontUserOutScopeValues = new DiagnosticDescriptor(
            nameof(DontUserOutScopeValues),
            "Don't use out scope values",
            "Variable {0} is out of scope and can't be used in the child query",
            "LinqQL",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}