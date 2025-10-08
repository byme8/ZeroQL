using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroQL.SourceGenerators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class QueryOnSyntaxAnalyzer : DiagnosticAnalyzer
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
            var onMethod = compilationContext.Compilation.GetTypeByMetadataName("ZeroQL.GraphQLSyntaxExtensions")?
                .GetMembers("On")
                .OfType<IMethodSymbol>()
                .FirstOrDefault();

            if (onMethod == null)
            {
                return; // ZeroQL not referenced in this compilation
            }

            compilationContext.RegisterSyntaxNodeAction(
                ctx => Handle(ctx, onMethod),
                SyntaxKind.InvocationExpression);
        });
    }

    private void Handle(SyntaxNodeAnalysisContext context, IMethodSymbol onMethod)
    {
        if (context.Node is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "On" } memberAccess
            } invocation)
        {
            return;
        }

        var possibleMethod = context.SemanticModel.GetSymbolInfo(invocation);
        if (possibleMethod.Symbol is not IMethodSymbol method)
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(onMethod, method.ReducedFrom))
        {
            return;
        }

        var typeArgument = method.TypeArguments.FirstOrDefault();
        if (typeArgument is null)
        {
            return;
        }

        var type = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
        if (type.Type is not INamedTypeSymbol targetType)
        {
            return;
        }

        if (typeArgument.AllInterfaces.Contains(targetType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.GraphQLQueryInvalidUnionType,
            memberAccess.Name.Identifier.GetLocation(),
            typeArgument.Name,
            targetType.Name));
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(Descriptors.GraphQLQueryInvalidUnionType);
}