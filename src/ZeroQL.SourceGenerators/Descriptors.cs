using Microsoft.CodeAnalysis;

namespace ZeroQL.SourceGenerators;

public class Descriptors
{
    public static readonly DiagnosticDescriptor UnexpectedFail = new(
        "ZQL0001",
        "Source generator failed unexpectedly",
        "Source generator failed unexpectedly with exception message:. {0}.",
        "ZeroQL",
        DiagnosticSeverity.Error,
        true);
    
    public static readonly DiagnosticDescriptor FailedToConvertPartOfTheQuery = new(
        "ZQL0002",
        "Failed to convert to graphql query",
        "Failed to convert to graphql query: {0}",
        "ZeroQL",
        DiagnosticSeverity.Error,
        true);
    
    public static readonly DiagnosticDescriptor FailedToConvert = new(
        "ZQL0003",
        "Failed to convert to graphql query",
        "{0}",
        "ZeroQL",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor DontUseOutScopeValues = new(
        "ZQL0004",
        "Don't use out scope values",
        "Variable {0} is out of scope and can't be used in the child query",
        "ZeroQL",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor OpenLambdaIsNotAllowed = new(
        "ZQL0005",
        "Open lambda are not allowed",
        "Open lambda like 'o => o' are not allowed. Use a lambda like 'o => new { o.Id }' or define a fragment.",
        "ZeroQL",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor OnlyStaticLambda = new(
        "ZQL0006",
        "Only static lambda are allowed",
        "The 'graphql' lambda should be static. It is required to transform it to a graphql query.",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor OnlyFieldSelectorsAndFragmentsAreAllowed = new(
        "ZQL0007",
        "Only field selectors and fragments are allowed",
        "The member doesn't have field selector or fragment attribute and can't be used in the query",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor FragmentsWithoutSyntaxTree = new(
        "ZQL0008",
        "The syntax tree is not detected",
        "The fragment is defined in different assembly. Make it partial to generate syntax tree independent fragment.",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor GraphQLQueryPreview = new(
        "ZQL0009",
        string.Empty,
        "{0}",
        "ZeroQL",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor GraphQLQueryNameShouldBeLiteral = new(
        "ZQL0010",
        "GraphQL query name should be literal or nameof expression",
        "GraphQL query name should be literal or nameof expression",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor GraphQLQueryInvalidUnionType = new(
        "ZQL0011",
        "Invalid union type",
        "Type {0} has to implement {1} interface",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor GraphQLVariableShouldBeLocal = new(
        "ZQL0012",
        "Properties and fields are not supported",
        "Define local variable to use it in the query. For example, 'var id = this.Id;'.",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor GraphQLVariableExpected = new(
        "ZQL0013",
        "Variable expected here",
        "Define local variable to use it in the query. For example, 'var filter = new Filter();'.",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor GraphQLQueryRequiredParameter = new(
        "ZQL0014",
        "Query requires parameter",
        "Query '{0}' requires parameter '{1}'",
        "ZeroQL",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

}