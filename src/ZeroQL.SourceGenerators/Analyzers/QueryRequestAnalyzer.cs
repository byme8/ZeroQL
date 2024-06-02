using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ZeroQL.SourceGenerators.Resolver.Context;

namespace ZeroQL.SourceGenerators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class QueryRequestAnalyzer : DiagnosticAnalyzer
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
        context.RegisterSyntaxNodeAction(Handle, SyntaxKind.RecordDeclaration);
    }

    private void Handle(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not RecordDeclarationSyntax record)
        {
            return;
        }

        var semanticModel = context.SemanticModel;
        var recordSymbol = semanticModel.GetDeclaredSymbol(record);
        var graphQLRequest = semanticModel.Compilation.GetTypeByMetadataName("ZeroQL.GraphQL`2");

        if (!SymbolEqualityComparer.Default.Equals(recordSymbol!.BaseType!.ConstructedFrom, graphQLRequest))
        {
            return;
        }

        var executeMethod = record.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == "Execute");

        if (executeMethod is null)
        {
            return;
        }

        var innerLambdas = executeMethod
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

        var resolver = new ZeroQLRequestLikeContextResolver();
        var (requestContext, resolveError) =
            resolver.Resolve(record, semanticModel, context.CancellationToken).Unwrap();
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
                    record.Identifier.GetLocation(),
                    resolveError.Message));
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.GraphQLQueryPreview,
            record.Identifier.GetLocation(),
            requestContext.OperationQuery));
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
            Descriptors.GraphQLQueryPreview);
}