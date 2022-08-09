using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.SourceGenerators.Resolver;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZeroQL.SourceGenerators.Generator;

[Generator]
public class GraphQLFragmentTemplateIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fragments = context.SyntaxProvider.CreateSyntaxProvider(
            FindFragmentThatRequiresSourceGeneration,
            (c, ct) =>
            {
                var attribute = (AttributeSyntax)c.Node;
                var method = (MethodDeclarationSyntax)attribute.Parent!.Parent!;

                return (Method: method, c.SemanticModel);
            });

        context.RegisterSourceOutput(fragments, AddMetadataForFragment);
    }

    private void AddMetadataForFragment(
        SourceProductionContext context,
        (MethodDeclarationSyntax Method, SemanticModel SemanticModel) input)
    {
        var (methodDeclaration, semanticModel) = input;
        if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration)
        {
            return;
        }

        if (classDeclaration.Parent is not BaseNamespaceDeclarationSyntax namespaceDeclaration)
        {
            return;
        }


        if (namespaceDeclaration.Parent is not CompilationUnitSyntax compilationUnit)
        {
            return;
        }

        var (graphQLTemplate, error) = GraphQLQueryResolver.ResolveFragmentTemplate(semanticModel, methodDeclaration, context.CancellationToken).Unwrap();
        if (error)
        {
            if (error is ErrorWithData<DiagnosticDescriptor> diagnosticError)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnosticError.Data, methodDeclaration.GetLocation()));
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptors.FailedToConvert, methodDeclaration.GetLocation()));
            return;
        }

        var usings = namespaceDeclaration.Usings.Concat(compilationUnit.Usings).ToArray();

        var literal = LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(graphQLTemplate));
        var newMethod = MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                .WithParameterList(methodDeclaration.ParameterList)
                .AddAttributeLists(AttributeList()
                        .AddAttributes(Attribute(ParseName(SourceGeneratorInfo.GraphQLQueryTemplateAttribute))
                            .WithArgumentList(ParseAttributeArgumentList($"({literal})"))))
                .WithModifiers(methodDeclaration.Modifiers)
            .WithBody(null)
            .WithSemicolonToken(ParseToken(";"));

        var syntaxTree = compilationUnit
            .WithUsings(List(usings))
            .WithMembers(List<MemberDeclarationSyntax>()
                .Add(NamespaceDeclaration(namespaceDeclaration.Name)
                    .WithMembers(List<MemberDeclarationSyntax>()
                        .Add(classDeclaration
                            .WithMembers(List<MemberDeclarationSyntax>()
                                .Add(newMethod))))));

        var source = syntaxTree
            .NormalizeWhitespace()
            .ToString();

        var uniqueId = Guid.NewGuid().ToString("N");
        context.AddSource($"{classDeclaration.Identifier.Text}.{uniqueId}.g.cs", source);
    }

    private bool FindFragmentThatRequiresSourceGeneration(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not AttributeSyntax attribute)
        {
            return false;
        }

        if (attribute.Name is not IdentifierNameSyntax name)
        {
            return false;
        }

        if (attribute.Parent is not AttributeListSyntax attributeList)
        {
            return false;
        }

        if (attributeList.Parent is not MethodDeclarationSyntax method)
        {
            return false;
        }

        if (method.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword)))
        {
            return false;
        }

        return name.Identifier.Text.EndsWith(SourceGeneratorInfo.GraphQLFragmentAttributeTypeName);
    }
}