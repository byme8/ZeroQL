using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ZeroQL.SourceGenerators.Generator;

namespace ZeroQL.SourceGenerators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class QueryLambdaAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
#if !DEBUG
            context.EnableConcurrentExecution();
#endif
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(Handle, SyntaxKind.InvocationExpression);
    }

    private void Handle(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Name.Identifier.ValueText is not "Query" or "Mutation")
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

        var possibleLambdaQuery = invocation.ArgumentList.Arguments.Last().Expression;
        if (possibleLambdaQuery is not LambdaExpressionSyntax lambda)
        {
            return;
        }

        if (!lambda.Modifiers.Any(SyntaxKind.StaticKeyword))
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
        var argumentSyntax = invocation.ArgumentList.Arguments.Last();
        var query = GraphQLQueryGenerator.Generate(semanticModel, argumentSyntax.Expression, context.CancellationToken);
        if (query.Error is ErrorWithData<Diagnostic> error)
        {
            context.ReportDiagnostic(error.Data);
        }
        else
        {
            context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.GraphQLQueryPreview,
                    memberAccess.Name.GetLocation(),
                    query.Value));
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(
            Descriptors.OnlyStaticLambda,
            Descriptors.OpenLambdaIsNotAllowed,
            Descriptors.DontUserOutScopeValues,
            Descriptors.FailedToConvert,
            Descriptors.OnlyFieldSelectorsAndFragmentsAreAllowed,
            Descriptors.GraphQLQueryPreview);
}