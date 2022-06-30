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
                var query = GetQuery(semanticModel, method, invocation);

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

        private string GetQuery(SemanticModel semanticModel, IMethodSymbol method, InvocationExpressionSyntax invocation)
        {
            var parameterNames = method.Parameters
                .Select(p => p.Name)
                .ToArray();

            var queryExpression = invocation.ArgumentList.Arguments.Last().Expression;
            if (parameterNames.SequenceEqual(new[] { "name", "query", "queryKey" }))
            {
                return GenerateGraphQLQuery(semanticModel, invocation.ArgumentList.Arguments.First().ToString(), queryExpression);
            }

            if (parameterNames.SequenceEqual(new[] { "query", "queryKey" }))
            {
                return GenerateGraphQLQuery(semanticModel, string.Empty, queryExpression);
            }

            return "query { }";
        }

        public string GenerateGraphQLQuery(SemanticModel semanticModel, string name, ExpressionSyntax query)
        {
            if (!(query is SimpleLambdaExpressionSyntax lambda))
            {
                return "";
            }

            var queryArgumentName = lambda.Parameter.Identifier.ValueText;
            var body = GenerateBody(semanticModel, queryArgumentName, lambda.Body);
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

        private string GenerateBody(SemanticModel semanticModel, string queryArgumentName, CSharpSyntaxNode lambdaBody)
        {
            switch (lambdaBody)
            {
                case InvocationExpressionSyntax invocation:
                {
                    var symbol = semanticModel.GetSymbolInfo(invocation);
                    if (!(symbol.Symbol is IMethodSymbol method))
                    {
                        return Failed(invocation);
                    }

                    var argumentNames = method.Parameters
                        .Take(method.Parameters.Length - 1)
                        .Select(o => $"${o.Name.FirstToLower()}: ")
                        .ToArray();
                    
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append(method.Name.FirstToLower());
                    if (argumentNames.Any())
                    {
                        var graphQLArguments = invocation.ArgumentList.Arguments
                            .Take(argumentNames.Length)
                            .Select((o, i) =>$"{argumentNames[i]}{GenerateBody(semanticModel, queryArgumentName, o)}")
                            .Join()
                            .Wrap("(", ")");
                        
                        stringBuilder.Append(graphQLArguments);
                    }
                    stringBuilder.Append($" {{ {GenerateBody(semanticModel, queryArgumentName, invocation.ArgumentList.Arguments.Last().Expression)} }} ");

                    return stringBuilder.ToString();
                }
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
                case SimpleLambdaExpressionSyntax simpleLambda:
                {
                    return GenerateBody(semanticModel, simpleLambda.Parameter.Identifier.ValueText, simpleLambda.Body);
                }
                case ArgumentSyntax argument:
                {
                    if (argument.Expression is LiteralExpressionSyntax literal)
                    {
                        return literal.ToString();
                    }

                    return Failed(argument);
                }
                case AnonymousObjectCreationExpressionSyntax anonymous:
                {
                    return anonymous.Initializers
                        .Select(o => GenerateBody(semanticModel, queryArgumentName, o))
                        .Join(" ");
                }
                case AnonymousObjectMemberDeclaratorSyntax anonymousMember:
                {
                    return GenerateBody(semanticModel, queryArgumentName, anonymousMember.Expression);
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