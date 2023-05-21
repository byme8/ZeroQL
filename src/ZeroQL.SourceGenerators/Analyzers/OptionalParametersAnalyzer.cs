using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

        var possibleMethod = context.SemanticModel.GetSymbolInfo(invocation);
        if (possibleMethod.Symbol is not IMethodSymbol method)
        {
            return;
        }

        var selectorAttribute = context.Compilation.GetTypeByMetadataName("ZeroQL.GraphQLFieldSelector")!;
        if (!method.GetAttributes().Any(o => SymbolEqualityComparer.Default.Equals(o.AttributeClass, selectorAttribute)))
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

        var arguments = invocation.ArgumentList.Arguments;
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
            
            // var argument = arguments[requiredParameter.Index];
            // var argumentType = context.SemanticModel.GetSymbolInfo(argument.Expression);
            // var type = argumentType.Symbol.GetNamedTypeSymbol();
            // if (type is null)
            // {
            //     continue;
            // }
            //
            // if (SymbolEqualityComparer.Default.Equals(type, requiredParameter.Parameter.Type))
            // {
            //     continue;
            // }
            //
            // context.ReportDiagnostic(Diagnostic.Create(
            //     Descriptors.GraphQLQueryRequiredParameter,
            //     memberAccess.Name.GetLocation(),
            //     method.Name,
            //     requiredParameter.Parameter.Name));
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(Descriptors.GraphQLQueryRequiredParameter);
}