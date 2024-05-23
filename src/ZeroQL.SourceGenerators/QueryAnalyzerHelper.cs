using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.SourceGenerators;

public class ArgumentAndIndex
{
    public int Index { get; set; }
    public IParameterSymbol Parameter { get; set; }
}

public class QueryAnalyzerHelper
{
    public static ArgumentAndIndex[] ExtractQueryMethod(
        Compilation compilation,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol attributeType)
    {
        var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return [];
        }

        var possibleMethod = semanticModel.GetSymbolInfo(memberAccess.Name);
        if (possibleMethod.Symbol is not IMethodSymbol method)
        {
            return [];
        }

        var graphQLLambdas = method.Parameters
            .Select((o, i) => new ArgumentAndIndex { Index = i, Parameter = o })
            .Where(o => o.Parameter
                .GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType)))
            .ToArray();

        var namedArguments = invocation.ArgumentList.Arguments
            .Select((o, i) => new { Index = i, Argument = o })
            .Where(o => o.Argument.NameColon is not null)
            .ToArray();

        if (namedArguments.Empty())
        {
            return graphQLLambdas;
        }

        foreach (var namedArgument in namedArguments)
        {
            var namedGraphQlLambda = graphQLLambdas
                .FirstOrDefault(o => o.Parameter.Name == namedArgument.Argument.NameColon!.Name.Identifier.ValueText);

            if (namedGraphQlLambda is null)
            {
                continue;
            }
            
            namedGraphQlLambda.Index = namedArgument.Index;
        }

        return graphQLLambdas;

        // var queryInfoProvider = compilation.GetTypeByMetadataName("ZeroQL.Stores.QueryInfoProvider");
        // if (SymbolEqualityComparer.Default.Equals(containingType,  queryInfoProvider))
        // {
        //     return method;
        // }
    }

    public static bool IsOpenLambda(LambdaExpressionSyntax lambda)
    {
        return lambda.Body is IdentifierNameSyntax;
    }
}