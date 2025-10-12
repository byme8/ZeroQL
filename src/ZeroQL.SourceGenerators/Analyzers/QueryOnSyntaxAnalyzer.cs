using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ZeroQL.SourceGenerators.Extensions;

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

            compilationContext.RegisterOperationAction(
                ctx => Handle(ctx, onMethod),
                OperationKind.Invocation);
        });
    }

    private void Handle(OperationAnalysisContext context, IMethodSymbol onMethod)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;

        // Check if this is the "On" method we're looking for
        if (!SymbolEqualityComparer.Default.Equals(onMethod, method.ConstructedFrom))
        {
            return;
        }

        var targetOperation = invocation.FirstRecursive(o => o is IParameterReferenceOperation);
        var typeArgument = method.TypeArguments.FirstOrDefault();
        if (typeArgument is null)
        {
            return;
        }

        // Get the receiver type (the expression that .On() is called on)
        if (targetOperation?.Type is not INamedTypeSymbol targetType)
        {
            return;
        }

        // Get the syntax node for location reporting
        if (invocation.Syntax is not InvocationExpressionSyntax invocationSyntax)
        {
            return;
        }

        if (invocationSyntax.Expression is not MemberAccessExpressionSyntax memberAccess)
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