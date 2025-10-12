using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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

        var graphQLLambdas = QueryAnalyzerHelper.ExtractQueryMethod(
            invocation.TargetMethod,
            graphQLLambdaAttribute,
            invocationSyntax);

        if (graphQLLambdas.Empty())
        {
            return;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        var arguments = invocationSyntax.ArgumentList.Arguments;
        var lambdas = graphQLLambdas
            .Select(o => (ArgumentInfo: o, Argument: arguments[o.Index]))
            .Select(o => (o.Argument, Expression: o.Argument.Expression as LambdaExpressionSyntax))
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

        var semanticModel = invocation.SemanticModel;
        if (semanticModel == null)
        {
            return;
        }

        var resolver = new GraphQLLambdaLikeContextResolver();
        var (result, resolveError) =
            resolver.Resolve(invocationSyntax, semanticModel, context.CancellationToken).Unwrap();

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
                    invocationSyntax.GetLocationForPreview(),
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
                invocationSyntax.GetLocationForPreview(),
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