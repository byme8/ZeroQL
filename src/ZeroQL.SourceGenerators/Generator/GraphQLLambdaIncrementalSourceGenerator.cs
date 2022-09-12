using System;
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
            .CreateSyntaxProvider(FindMethods, (c, ct) => (Invocation: (InvocationExpressionSyntax)c.Node, c.SemanticModel));

        context.RegisterImplementationSourceOutput(invocations, GenerateSource);
    }

    private void GenerateSource(
        SourceProductionContext context,
        (InvocationExpressionSyntax Invocation, SemanticModel SemanticModel) input)
    {
        var (invocation, semanticModel) = input;
        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

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
                    Descriptors.FailedToConvert,
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

        var uniqId = Guid.NewGuid().ToString("N");
        var source = GraphQLSourceResolver.Resolve(
            semanticModel,
            uniqId,
            lambdaContext);

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        context.AddSource($"ZeroQLModuleInitializer.{uniqId}.g.cs", source);
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