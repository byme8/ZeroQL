using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ZeroQL.SourceGenerators.Extensions;
using ZeroQL.SourceGenerators.Resolver.Context;

namespace ZeroQL.SourceGenerators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class QueryLambdaAnalyzer : DiagnosticAnalyzer
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

            if (graphQLClient == null)
            {
                return; // ZeroQL not referenced in this compilation
            }

            compilationContext.RegisterSyntaxNodeAction(
                Handle,
                SyntaxKind.InvocationExpression);
        });
    }

    private void Handle(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        var graphQLLambdas = QueryAnalyzerHelper.ExtractQueryMethod(
            context.Compilation,
            invocation);

        if (graphQLLambdas.Empty())
        {
            return;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        var lambdas = graphQLLambdas
            .Select(o => invocation.ArgumentList.Arguments[o.Index])
            .Select(o => (Argument: o, Expression: o.Expression as LambdaExpressionSyntax))
            .Where(o => o.Expression is not null)
            .ToArray();

        if (lambdas.Empty())
        {
            return;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        var semanticModel = context.SemanticModel;
        var resolver = new GraphQLLambdaLikeContextResolver();
        var (result, resolveError) =
            resolver.Resolve(invocation, semanticModel, context.CancellationToken).Unwrap();

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (resolveError is ErrorWithData<Diagnostic> error)
        {
            context.ReportDiagnostic(error.Data);
            return;
        }

        if (resolveError)
        {
            context.ReportDiagnostic(Diagnostic
                .Create(
                    Descriptors.FailedToConvert,
                    invocation.GetLocationForPreview(),
                    resolveError.Message));
            return;
        }

        if (result.LambdaContexts is null)
        {
            return;
        }

        foreach (var lambdaContext in result.LambdaContexts)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.GraphQLQueryPreview,
                invocation.GetLocationForPreview(),
                lambdaContext.OperationQuery));
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(
            Descriptors.FragmentsWithoutSyntaxTree,
            Descriptors.OpenLambdaIsNotAllowed,
            Descriptors.DontUseOutScopeValues,
            Descriptors.FailedToConvertPartOfTheQuery,
            Descriptors.FailedToConvert,
            Descriptors.OnlyFieldSelectorsAndFragmentsAreAllowed,
            Descriptors.GraphQLQueryNameShouldBeLiteral,
            Descriptors.GraphQLQueryPreview,
            Descriptors.GraphQLVariableShouldBeLocal,
            Descriptors.GraphQLVariableExpected);
}