using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.SourceGenerators;

public class QueryAnalyzerHelper
{
    public static IMethodSymbol? ExtractQueryMethod(Compilation compilation, InvocationExpressionSyntax invocation)
    {
        var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return null;
        }

        var lambdaExtensions = compilation.GetTypeByMetadataName("GraphQLClientLambdaExtensions");
        var possibleMethod = semanticModel.GetSymbolInfo(memberAccess.Name);
        if (possibleMethod.Symbol is not IMethodSymbol { ContainingSymbol: INamedTypeSymbol containingType } method ||
            !SymbolEqualityComparer.Default.Equals(containingType,  lambdaExtensions))
        {
            return null;
        }

        return method;
    }

    public static bool IsOpenLambda(LambdaExpressionSyntax lambda)
    {
        return lambda.Body is IdentifierNameSyntax;
    }
}