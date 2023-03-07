using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.SourceGenerators.Resolver;
using ZeroQL.SourceGenerators.Resolver.Context;

namespace ZeroQL.SourceGenerators.Generator;

[Generator]
public class GraphQLLambdaIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var invocations = context.SyntaxProvider
            .CreateSyntaxProvider(FindMethods, (c, _) => (Invocation: (InvocationExpressionSyntax)c.Node, c.SemanticModel));

        var collectedInvocations = invocations.Collect();

        context.RegisterImplementationSourceOutput(collectedInvocations, GenerateSource);
    }

    private void GenerateSource(
        SourceProductionContext context,
        ImmutableArray<(InvocationExpressionSyntax Invocation, SemanticModel SemanticModel)> invocations)
    {
        var processed = new HashSet<string>();
        foreach (var input in invocations)
        {
            var (invocation, semanticModel) = input;
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            Utils.ErrorWrapper(
                context,
                invocation,
                () => GenerateFile(context, invocation, semanticModel, processed));
        }
    }

    private static void GenerateFile(SourceProductionContext context, InvocationExpressionSyntax invocation, SemanticModel semanticModel, HashSet<string> processed)
    {
        var resolver = new GraphQLLambdaLikeContextResolver();
        var (lambdaContext, error) = resolver.Resolve(invocation, semanticModel, context.CancellationToken).Unwrap();
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

        var source = GraphQLSourceResolver.Resolve(
            semanticModel,
            lambdaContext);

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (processed.Contains(lambdaContext.OperationHash))
        {
            return;
        }

        processed.Add(lambdaContext.OperationHash);
        context.AddSource($"ZeroQLModuleInitializer.{lambdaContext.OperationHash}.g.cs", source);
    }

    private bool FindMethods(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.ToString() is "Query" or "Mutation")
        {
            return true;
        }

        return false;
    }
}