using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LinqQL.SourceGenerators.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OnlyStaticLambdaAnalyzer : DiagnosticAnalyzer
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
                memberAccess.Name.Identifier.ValueText != "Query")
            {
                return;
            }

            var method = GraphQLQueryAnalyzerHelper.ExtractQueryMethod(context.Compilation, invocation);
            if (method is null)
            {
                return;
            }

            var possibleLambdaQuery = invocation.ArgumentList.Arguments.Last().Expression;
            if (possibleLambdaQuery is not SimpleLambdaExpressionSyntax lambda)
            {
                return;
            }

            if (!lambda.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.OnlyStaticLambda,
                    lambda.GetLocation()));
            }
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(Descriptors.OnlyStaticLambda);
    }
}