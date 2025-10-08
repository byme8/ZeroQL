using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        context.RegisterSyntaxNodeAction(Handle, SyntaxKind.InvocationExpression);
    }

    private void Handle(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var staticLambdas = QueryAnalyzerHelper.ExtractQueryMethod(
            context.Compilation,
            invocation);

        if (staticLambdas.Empty())
        {
            return;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        var lambdas = staticLambdas
            .Select(o => invocation.ArgumentList.Arguments[o.Index])
            .Select(o => (Argument: o, Expression: o.Expression as LambdaExpressionSyntax))
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