using Microsoft.CodeAnalysis;

namespace ZeroQL.SourceGenerators;

public class Descriptors
{
    public static DiagnosticDescriptor FailedToConvert = new(
        nameof(FailedToConvert),
        "Failed to convert to graphql query",
        "Failed to convert to graphql query: {0}",
        "ZeroQL",
        DiagnosticSeverity.Error,
        true);

    public static DiagnosticDescriptor DontUseOutScopeValues = new(
        nameof(DontUseOutScopeValues),
        "Don't use out scope values",
        "Variable {0} is out of scope and can't be used in the child query",
        "ZeroQL",
        DiagnosticSeverity.Error,
        true);

    public static DiagnosticDescriptor OpenLambdaIsNotAllowed = new(
        nameof(OpenLambdaIsNotAllowed),
        "Open lambda are not allowed",
        "Open lambda like 'o => o' are not allowed. Use a lambda like 'o => new { o.Id }' or define a fragment.",
        "ZeroQL",
        DiagnosticSeverity.Error,
        true);

    public static DiagnosticDescriptor OnlyStaticLambda = new(
        nameof(OnlyStaticLambda),
        "Only static lambda are allowed",
        "The 'graphql' lambda should be static. It is required to transform it to a graphql query.",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static DiagnosticDescriptor OnlyFieldSelectorsAndFragmentsAreAllowed = new(
        nameof(OnlyFieldSelectorsAndFragmentsAreAllowed),
        "Only field selectors and fragments are allowed",
        "The method doesn't have field selector or fragment attribute and can't be used in the query",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static DiagnosticDescriptor FragmentsWithoutSyntaxTree = new(
        nameof(FragmentsWithoutSyntaxTree),
        "The syntax tree is not detected.",
        "The fragment is defined in different assembly. Make it partial to generate syntax tree independent fragment.",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static DiagnosticDescriptor GraphQLQueryPreview = new(
        nameof(GraphQLQueryPreview),
        string.Empty,
        "{0}",
        "ZeroQL",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);
    
    public static DiagnosticDescriptor GraphQLQueryNameShouldBeLiteral = new(
        nameof(GraphQLQueryNameShouldBeLiteral),
        "GraphQL query name should be literal.",
        "GraphQL query name should be literal.",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}