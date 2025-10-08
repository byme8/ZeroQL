using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.SourceGenerators.Extensions;

public static class InvocationExtensions
{
    public static bool PotentialGraphQLLambda(this InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.ValueText;
            return methodName.Contains("Query") ||
                   methodName.Contains("Mutation") ||
                   methodName.Contains("Materialize");
        }

        if (invocation.Expression is IdentifierNameSyntax identifier)
        {
            var methodName = identifier.Identifier.ValueText;
            return methodName.Contains("Query") ||
                   methodName.Contains("Mutation") ||
                   methodName.Contains("Materialize");
        }

        return false;
    }
}