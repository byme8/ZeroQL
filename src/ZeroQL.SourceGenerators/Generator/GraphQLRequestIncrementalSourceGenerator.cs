using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.SourceGenerators.Resolver;
using ZeroQL.SourceGenerators.Resolver.Context;

namespace ZeroQL.SourceGenerators.Generator;

[Generator]
public class GraphQLRequestIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var invocations = context.SyntaxProvider
            .CreateSyntaxProvider(FindGraphQLRequests,
                (c, ct) => (Record: (RecordDeclarationSyntax)c.Node, c.SemanticModel));

        var collectedInvocations = invocations.Collect();

        context.RegisterImplementationSourceOutput(collectedInvocations, GenerateSource);
    }

    private void GenerateSource(SourceProductionContext context,
        ImmutableArray<(RecordDeclarationSyntax Record, SemanticModel SemanticModel)> invocations)
    {
        var processed = new HashSet<string>();
        foreach (var input in invocations)
        {
            var (record, semanticModel) = input;
            Utils.ErrorWrapper(
                context,
                record,
                () => GenerateFile(context, semanticModel, record, processed));
        }
    }

    private void GenerateFile(SourceProductionContext context, SemanticModel semanticModel, RecordDeclarationSyntax record, HashSet<string> processed)
    {
        var recordSymbol = semanticModel.GetDeclaredSymbol(record);
        var graphQLRequest = semanticModel.Compilation.GetTypeByMetadataName("ZeroQL.GraphQL`2");

        if (!SymbolEqualityComparer.Default.Equals(recordSymbol!.BaseType!.ConstructedFrom, graphQLRequest))
        {
            return;
        }

        var resolver = new ZeroQLRequestLikeContextResolver();
        var (requestLikeContext, error) = resolver.Resolve(record, semanticModel, context.CancellationToken).Unwrap();
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
                    record
                        .Members
                        .OfType<MethodDeclarationSyntax>()
                        .First()
                        .GetLocation()));
            return;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        var source = GraphQLSourceResolver.Resolve(semanticModel, requestLikeContext);

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (processed.Contains(requestLikeContext.OperationHash))
        {
            return;
        }

        processed.Add(requestLikeContext.OperationHash);
        context.AddSource($"ZeroQLModuleInitializer.{requestLikeContext.OperationHash}.g.cs", source);
    }

    private bool FindGraphQLRequests(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is RecordDeclarationSyntax recordDeclaration)
        {
            var possibleGraphQLRequest = recordDeclaration.BaseList?.Types
                .FirstOrDefault(type => type is SimpleBaseTypeSyntax
                {
                    Type: GenericNameSyntax { Identifier.Text: "GraphQL" }
                });

            return possibleGraphQLRequest is not null;
        }

        return false;
    }
}