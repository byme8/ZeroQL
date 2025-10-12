using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ZeroQL.SourceGenerators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OptionalParametersAnalyzer : DiagnosticAnalyzer
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
            var nameAttribute = compilationContext.Compilation
                .GetTypeByMetadataName(SourceGeneratorInfo.GraphQLNameAttribute);

            if (nameAttribute == null)
            {
                return; // ZeroQL not referenced in this compilation
            }

            compilationContext.RegisterOperationAction(
                ctx => Handle(ctx, nameAttribute),
                OperationKind.Invocation);
        });
    }

    private void Handle(OperationAnalysisContext context, INamedTypeSymbol nameAttribute)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;
        if (!method.GetAttributes().Any(o => SymbolEqualityComparer.Default.Equals(o.AttributeClass, nameAttribute)))
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

        var requiredParameters = method.Parameters
            .Select((o, i) => (Parameter: o, Index: i))
            .Where(o => o.Parameter.Type.NullableAnnotation == NullableAnnotation.NotAnnotated)
            .ToArray();

        if (requiredParameters.Length == 0)
        {
            return;
        }

        var arguments = invocationSyntax.ArgumentList.Arguments;
        for (int i = 0; i < requiredParameters.Length; i++)
        {
            var requiredParameter = requiredParameters[i];
            var requiredParameterNamed = arguments
                .Any(o => o.NameColon?.Name.Identifier.ValueText == requiredParameter.Parameter.Name);

            if (requiredParameterNamed)
            {
                continue;
            }

            if (arguments.Count <= requiredParameter.Index)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.GraphQLQueryRequiredParameter,
                    memberAccess.Name.GetLocation(),
                    method.Name,
                    requiredParameter.Parameter.Name));
            }
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(Descriptors.GraphQLQueryRequiredParameter);
}