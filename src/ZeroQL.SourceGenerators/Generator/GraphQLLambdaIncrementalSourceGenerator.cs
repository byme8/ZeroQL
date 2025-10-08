using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using ZeroQL.Extensions;
using ZeroQL.SourceGenerators.Extensions;
using ZeroQL.SourceGenerators.Resolver;
using ZeroQL.SourceGenerators.Resolver.Context;

namespace ZeroQL.SourceGenerators.Generator;

[Generator]
public class GraphQLLambdaIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var invocations = context.SyntaxProvider
            .CreateSyntaxProvider(FindMethods,
                (c, _) => (Invocation: (InvocationExpressionSyntax)c.Node, c.SemanticModel));

        IncrementalValueProvider<string?> rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select((AnalyzerConfigOptionsProvider c, CancellationToken _) =>
                c.GlobalOptions.TryGetValue("build_property.RootNamespace", out var nameSpace)
                    ? nameSpace
                    : null);

        var collectedInvocations = invocations.Combine(rootNamespace)
            .Select((item, token) => (item.Left.Invocation, item.Left.SemanticModel, item.Right))
            .Collect();

        context.RegisterImplementationSourceOutput(collectedInvocations, GenerateSource);
    }

    private void GenerateSource(
        SourceProductionContext context,
        ImmutableArray<(InvocationExpressionSyntax Invocation, SemanticModel SemanticModel, string? RootNamespace)>
            invocations)
    {
        var processed = new HashSet<string>();
        foreach (var input in invocations)
        {
            var (invocation, semanticModel, rootNamespace) = input;
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            Utils.ErrorWrapper(
                context,
                invocation,
                () => GenerateFile(context, invocation,
                    (rootNamespace ?? semanticModel.Compilation.Assembly.Name).ToSafeNamespace(), semanticModel,
                    processed));
        }
    }

    private static void GenerateFile(
        SourceProductionContext context,
        InvocationExpressionSyntax invocation,
        string rootNamespace,
        SemanticModel semanticModel,
        HashSet<string> processed)
    {
        var resolver = new GraphQLLambdaLikeContextResolver();
        var (result, error) = resolver.Resolve(invocation, semanticModel, context.CancellationToken).Unwrap();
        if (error)
        {
            if (error is ErrorWithData<Diagnostic> errorWithData)
            {
                context.ReportDiagnostic(errorWithData.Data);
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.FailedToConvertPartOfTheQuery,
                    invocation
                        .ArgumentList
                        .Arguments
                        .First()
                        .Expression
                        .GetLocation()));
            return;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (result.LambdaContexts is null)
        {
            return;
        }

        foreach (var lambdaContext in result.LambdaContexts)
        {
            var source = GraphQLSourceResolver.Resolve(
                rootNamespace,
                lambdaContext);

            if (context.CancellationToken.IsCancellationRequested)
            {
                continue;
            }

            if (processed.Contains(lambdaContext.KeyHash))
            {
                continue;
            }

            processed.Add(lambdaContext.KeyHash);
            context.AddSource($"ZeroQLModuleInitializer.{lambdaContext.KeyHash}.g.cs", source.NormalizeLineEndings());
        }
    }

    public static bool FindMethods(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is InvocationExpressionSyntax invocation)
        {
            return invocation.PotentialGraphQLLambda();
        }
        
        return false;
    }
}