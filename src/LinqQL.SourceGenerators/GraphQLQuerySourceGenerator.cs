using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LinqQL.SourceGenerators
{
    [Generator]
    public class GraphQLQuerySourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new QueryMethodSelector());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is QueryMethodSelector receiver))
            {
                return;
            }

            var queries = new Dictionary<string, string>();
            foreach (var invocation in receiver.Invocations)
            {
                if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
                {
                    break;
                }

                var semanticModel = context.Compilation.GetSemanticModel(invocation.SyntaxTree);
                var possibleMethod = ModelExtensions.GetSymbolInfo(semanticModel, memberAccess.Name);
                if (!(possibleMethod.Symbol is IMethodSymbol method) ||
                    !(method.ContainingSymbol is INamedTypeSymbol containingType) ||
                    containingType.ConstructedFrom.ToString() != "LinqQL.Core.GraphQLClient<TQuery>")
                {
                    break;
                }

                var key = invocation.ArgumentList.Arguments.Last().ToString();
                var query = GetQuery(method, invocation);

                queries[key] = query;
            }

            var source = @$"using System;
using LinqQL.Core;

namespace {context.Compilation.Assembly.Name}
{{
    public static class LinqQLModuleInitializer
    {{
        [global::System.Runtime.CompilerServices.ModuleInitializer]
        public static void Init()
        {{
{queries.Select(o => $@"            GraphQLQueryStore.Query.Add(""{o.Key}"", ""{o.Value}"");").JoinWithNewLine()}
        }}
    }}
}}";

            context.AddSource("LinqQLModuleInitializer.g.cs", source);
        }

        private string GetQuery(IMethodSymbol method, InvocationExpressionSyntax invocation)
        {
            var parameterNames = method.Parameters
                .Select(p => p.Name)
                .ToArray();

            var queryExpression = invocation.ArgumentList.Arguments.Last().Expression;
            if (parameterNames.SequenceEqual(new[] { "name", "query" }))
            {
                return GenerateGraphQLQuery(invocation.ArgumentList.Arguments.First().ToString(), queryExpression);
            }

            if (parameterNames.SequenceEqual(new[] { "query" }))
            {
                return GenerateGraphQLQuery(string.Empty, queryExpression);
            }

            return "query { }";
        }

        public string GenerateGraphQLQuery(string name, ExpressionSyntax query)
        {
            if (!(query is SimpleLambdaExpressionSyntax lambda))
            {
                return "";
            }

            var queryArgumentName = lambda.Parameter.Identifier.ValueText;
            var body = GenerateBody(queryArgumentName, lambda.Body);
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("query");
            if (!string.IsNullOrEmpty(name))
            {
                stringBuilder.Append($" {name}");
            }
            stringBuilder.Append(" { ");
            stringBuilder.Append(body);
            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }

        private string GenerateBody(string queryArgumentName, CSharpSyntaxNode lambdaBody)
        {
            switch (lambdaBody)
            {
                case InvocationExpressionSyntax invocation:
                    return GenerateBody(queryArgumentName, invocation.Expression) + $" {{ {GenerateBody(queryArgumentName, invocation.ArgumentList)} }} ";
                case MemberAccessExpressionSyntax member:
                {
                    return member.Name.Identifier.ValueText.FirstToLower();
                }
                case IdentifierNameSyntax identifierNameSyntax:
                {
                    if (identifierNameSyntax.Identifier.ValueText == queryArgumentName)
                    {
                        return string.Empty;
                    }

                    return Failed(lambdaBody);
                }
                case ArgumentListSyntax argumentList:
                {
                    var argument = argumentList.Arguments.First();
                    if (!(argument.Expression is SimpleLambdaExpressionSyntax simpleLambda))
                    {
                        return Failed(lambdaBody);
                    }

                    return GenerateBody(simpleLambda.Parameter.Identifier.ValueText, simpleLambda.Body);
                }
            }

            return Failed(lambdaBody);
        }

        private static string Failed(CSharpSyntaxNode lambdaBody)
        {

            return $"// Failed to generate query for: {lambdaBody.ToString()}";
        }
    }

    public class QueryMethodSelector : ISyntaxReceiver
    {
        public List<InvocationExpressionSyntax> Invocations { get; } = new List<InvocationExpressionSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.ToString() == "Query")
            {
                Invocations.Add(invocation);
            }
        }
    }

}