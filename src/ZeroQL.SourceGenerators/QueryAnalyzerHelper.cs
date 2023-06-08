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

        var possibleMethod = semanticModel.GetSymbolInfo(memberAccess.Name);
        if (possibleMethod.Symbol is not IMethodSymbol { ContainingSymbol: INamedTypeSymbol containingType } method)
        {
            return null;
        }

        var lambdaExtensions = compilation.GetTypeByMetadataName("GraphQLClientLambdaExtensions");
        if (SymbolEqualityComparer.Default.Equals(containingType,  lambdaExtensions))
        {
            return method;
        }
        
        var queryInfoProvider = compilation.GetTypeByMetadataName("ZeroQL.Stores.QueryInfoProvider");
        if (SymbolEqualityComparer.Default.Equals(containingType,  queryInfoProvider))
        {
            return method;
        }

        return null;
    }

    public static bool IsOpenLambda(LambdaExpressionSyntax lambda)
    {
        return lambda.Body is IdentifierNameSyntax;
    }
}