using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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
        context.RegisterSyntaxNodeAction(Handle, SyntaxKind.InvocationExpression);
    }

    private void Handle(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Name.Identifier.ValueText is not ("Query" or "Mutation"))
        {
            return;
        }

        var method = QueryAnalyzerHelper.ExtractQueryMethod(context.Compilation, invocation);
        if (method is null)
        {
            return;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        var possibleLambdaQuery = invocation.ArgumentList.Arguments
            .LastOrDefault(o => o.Expression is LambdaExpressionSyntax)?
            .Expression;
        if (possibleLambdaQuery is not LambdaExpressionSyntax lambda)
        {
            return;
        }

        if (lambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda
            && !parenthesizedLambda.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.OnlyStaticLambda,
                lambda.GetLocation()));
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        var innerLambdas = lambda
            .DescendantNodes()
            .OfType<LambdaExpressionSyntax>()
            .ToArray();

        foreach (var innerLambda in innerLambdas)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (QueryAnalyzerHelper.IsOpenLambda(innerLambda))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.OpenLambdaIsNotAllowed,
                        innerLambda.GetLocation()));
            }
        }

        var semanticModel = context.SemanticModel;
        var resolver = new GraphQLLambdaLikeContextResolver();
        var (lambdaContext, resolveError) =
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
                    memberAccess.Name.GetLocation(),
                    resolveError.Code));
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.GraphQLQueryPreview,
            memberAccess.Name.GetLocation(),
            lambdaContext.OperationQuery));
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(
            Descriptors.OnlyStaticLambda,
            Descriptors.FragmentsWithoutSyntaxTree,
            Descriptors.OpenLambdaIsNotAllowed,
            Descriptors.DontUseOutScopeValues,
            Descriptors.FailedToConvertPartOfTheQuery,
            Descriptors.FailedToConvert,
            Descriptors.OnlyFieldSelectorsAndFragmentsAreAllowed,
            Descriptors.GraphQLQueryNameShouldBeLiteral,
            Descriptors.GraphQLQueryPreview);
}