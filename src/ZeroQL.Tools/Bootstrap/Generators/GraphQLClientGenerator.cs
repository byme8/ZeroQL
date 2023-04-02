using GraphQLParser.AST;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Internal;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZeroQL.Bootstrap.Generators;

public static class GraphQLClientGenerator
{
    public static ClassDeclarationSyntax GenerateClient(this GraphQlGeneratorOptions options, GraphQLNamedType? queryType,
        GraphQLNamedType? mutationType)
    {
        var clientName = options.ClientName;
        var queryTypeName = queryType?.Name.StringValue ?? "ZeroQL.Unit";
        var mutationTypeName = mutationType?.Name.StringValue ?? "ZeroQL.Unit";

        return CSharpHelper.Class(clientName ?? "GraphQLClient", options.Visibility)
            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(IdentifierName($"global::ZeroQL.GraphQLClient<{queryTypeName}, {mutationTypeName}>")))))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(
                ConstructorDeclaration(clientName ?? "GraphQLClient")
                    .WithParameterList(ParseParameterList(
                        "(global::System.Net.Http.HttpClient client, global::ZeroQL.Pipelines.IGraphQLQueryPipeline? queryPipeline = null)"))
                    // call base constructor
                    .WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(SeparatedList<ArgumentSyntax>()
                            .Add(Argument(IdentifierName("client")))
                            .Add(Argument(IdentifierName("queryPipeline")))
                        )))
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithBody(Block())));
    }
}