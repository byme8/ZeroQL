using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ZeroQL.SourceGenerators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StaticLambdaAnalyzer : DiagnosticAnalyzer
{
#pragma warning disable RS1026
    public override void Initialize(AnalysisContext context)
#pragma warning restore RS1026
    {
#if !DEBUG
            context.EnableConcurrentExecution();
#endif
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                               GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Check if ZeroQL types are available in this compilation
            var graphQLClient = compilationContext.Compilation.GetTypeByMetadataName("ZeroQL.IGraphQLClient");
            var graphQLLambdaAttribute = compilationContext.Compilation.GetTypeByMetadataName(SourceGeneratorInfo.GraphQLLambdaAttribute);

            if (graphQLClient == null || graphQLLambdaAttribute == null)
            {
                return; // ZeroQL not referenced in this compilation
            }

            compilationContext.RegisterOperationAction(
                ctx => Handle(ctx, graphQLLambdaAttribute),
                OperationKind.Invocation);
        });
    }

    private void Handle(OperationAnalysisContext context, INamedTypeSymbol graphQLLambdaAttribute)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        if (invocation.Syntax is not InvocationExpressionSyntax invocationSyntax)
        {
            return;
        }

        if (invocationSyntax.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var staticLambdas = QueryAnalyzerHelper.ExtractQueryMethod(
            invocation.TargetMethod,
            graphQLLambdaAttribute,
            invocationSyntax);

        if (staticLambdas.Empty())
        {
            return;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        var arguments = invocationSyntax.ArgumentList.Arguments;
        var lambdas = staticLambdas
            .Where(o => o.Index < arguments.Count)
            .Select(o => (ArgumentInfo: o, Argument: arguments[o.Index]))
            .Select(o => (o.Argument, Expression: o.Argument.Expression as LambdaExpressionSyntax))
            .Where(o => o.Expression is not null)
            .ToArray();

        if (lambdas.Empty())
        {
            return;
        }

        foreach (var lambda in lambdas)
        {
            if (lambda.Expression is ParenthesizedLambdaExpressionSyntax parenthesizedLambda
                && !parenthesizedLambda.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.OnlyStaticLambda,
                    lambda.Expression.GetLocation()));
            }
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(Descriptors.OnlyStaticLambda);
}