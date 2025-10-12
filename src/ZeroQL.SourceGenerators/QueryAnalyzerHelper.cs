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
        InvocationExpressionSyntax invocation)
    {
        var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);
        var attributeType = semanticModel.Compilation.GetTypeByMetadataName(SourceGeneratorInfo.GraphQLLambdaAttribute)!;

        var possibleMethod = semanticModel.GetSymbolInfo(invocation.Expression);
        if (possibleMethod.Symbol is not IMethodSymbol method)
        {
            return [];
        }

        return ExtractQueryMethod(method, attributeType, invocation);
    }

    public static ArgumentAndIndex[] ExtractQueryMethod(
        IMethodSymbol method,
        INamedTypeSymbol graphQLLambdaAttribute,
        InvocationExpressionSyntax invocation)
    {
        var isExtensionMethod = method.IsExtensionMethod;
        var currentMethod =  method.ReducedFrom ?? method;
        var graphQLLambdas = currentMethod.Parameters
            .Select((o, i) => new ArgumentAndIndex { Index = isExtensionMethod ? i - 1 : i, Parameter = o })
            .Where(o => o.Parameter
                .GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, graphQLLambdaAttribute)))
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
    }

    public static bool IsOpenLambda(LambdaExpressionSyntax lambda)
    {
        return lambda.Body is IdentifierNameSyntax;
    }
}