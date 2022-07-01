using System;
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
                return GenerateGraphQLQuery(semanticModel, invocation.ArgumentList.Arguments.First().ToString(), null, queryExpression);
            }

            if (parameterNames.SequenceEqual(new[] { "variables", "query", "queryKey" }))
            {
                var variablesExpression = invocation.ArgumentList.Arguments.First().Expression;
                return GenerateGraphQLQuery(semanticModel, string.Empty, variablesExpression, queryExpression);
            }

            if (parameterNames.SequenceEqual(new[] { "query", "queryKey" }))
            {
                return GenerateGraphQLQuery(semanticModel, string.Empty, null, queryExpression);
            }

            return "query { }";
        }

        public string GenerateGraphQLQuery(SemanticModel semanticModel, string name, ExpressionSyntax? variablesExpression, ExpressionSyntax query)
        {
            if (!(query is LambdaExpressionSyntax lambda))
            {
                return "";
            }

            var inputs = GetQueryInputs(lambda);
            var variables = GetVariables(semanticModel, variablesExpression);
            var availableVariables = inputs.VariablesName is null ? new Dictionary<string, string>()
                : variables
                    .ToDictionary(
                        o => $"{inputs.VariablesName}.{o.Name}",
                        o => "$" + o.Name.FirstToLower());

            var context = new GraphQLQueryGenerationContext
            {
                QueryVariableName = inputs.QueryName,
                AvailableVariables = availableVariables,
                SemanticModel = semanticModel
            };

            var body = GenerateBody(context, lambda.Body);
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("query");
            if (!string.IsNullOrEmpty(name))
            {
                stringBuilder.Append($" {name}");
            }
            if (inputs.VariablesName != null)
            {
                var variablesBody = variables
                    .Select(o => $"${o.Name.FirstToLower()}: {o.Type}")
                    .Join()
                    .Wrap(" (", ")");

                stringBuilder.Append(variablesBody);
            }
            stringBuilder.Append(" { ");
            stringBuilder.Append(body);
            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }

        private static (string? VariablesName, string QueryName) GetQueryInputs(LambdaExpressionSyntax lambda)
        {
            if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
            {
                return (null, simpleLambda.Parameter.Identifier.ValueText);
            }

            if (lambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
            {
                var parameters = parenthesizedLambda.ParameterList.Parameters;
                return (parameters.First().Identifier.ValueText, parameters.Last().Identifier.ValueText);
            }

            return default;
        }

        private (string Name, string Type)[] GetVariables(SemanticModel semanticModel, ExpressionSyntax? variablesExpression)
        {
            if (!(variablesExpression is AnonymousObjectCreationExpressionSyntax anonymousObject))
            {
                Failed(variablesExpression);
                return Array.Empty<(string Name, string Type)>();
            }

            var ctor = semanticModel.GetSymbolInfo(anonymousObject).Symbol as IMethodSymbol;
            var type = ctor.ContainingType;
            return type.GetMembers()
                .OfType<IPropertySymbol>()
                .Select(o => (o.Name, o.Type.ToStringWithNullable()))
                .ToArray();
        }

        private string GenerateBody(GraphQLQueryGenerationContext context, CSharpSyntaxNode lambdaBody)
        {
            switch (lambdaBody)
            {
                case InvocationExpressionSyntax invocation:
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(invocation);
                    if (!(symbol.Symbol is IMethodSymbol method))
                    {
                        return Failed(invocation);
                    }

                    var argumentNames = method.Parameters
                        .Take(method.Parameters.Length - 1)
                        .Select(o => $"{o.Name.FirstToLower()}: ")
                        .ToArray();

                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append(method.Name.FirstToLower());
                    if (argumentNames.Any())
                    {
                        var graphQLArguments = invocation.ArgumentList.Arguments
                            .Take(argumentNames.Length)
                            .Select((o, i) => $"{argumentNames[i]}{GenerateBody(context, o)}")
                            .Join()
                            .Wrap("(", ")");

                        stringBuilder.Append(graphQLArguments);
                    }
                    stringBuilder.Append($" {{ {GenerateBody(context, invocation.ArgumentList.Arguments.Last().Expression)} }} ");

                    return stringBuilder.ToString();
                }
                case MemberAccessExpressionSyntax member:
                {
                    return member.Name.Identifier.ValueText.FirstToLower();
                }
                case IdentifierNameSyntax identifierNameSyntax:
                {
                    if (identifierNameSyntax.Identifier.ValueText == context.QueryVariableName)
                    {
                        return string.Empty;
                    }

                    return Failed(lambdaBody);
                }
                case SimpleLambdaExpressionSyntax simpleLambda:
                {
                    return GenerateBody(context, simpleLambda.Body);
                }
                case ArgumentSyntax argument:
                {
                    if (argument.Expression is LiteralExpressionSyntax literal)
                    {
                        return literal.ToString();
                    }

                    var value = argument.Expression.ToString();
                    if (context.AvailableVariables.ContainsKey(value))
                    {
                        return context.AvailableVariables[value];
                    }

                    return Failed(argument);
                }
                case AnonymousObjectCreationExpressionSyntax anonymous:
                {
                    return anonymous.Initializers
                        .Select(o => GenerateBody(context, o))
                        .Join(" ");
                }
                case AnonymousObjectMemberDeclaratorSyntax anonymousMember:
                {
                    return GenerateBody(context, anonymousMember.Expression);
                }
            }

            return Failed(lambdaBody);
        }

        private static string Failed(CSharpSyntaxNode? lambdaBody)
        {
            return $"// Failed to generate query for: {lambdaBody?.ToString() ?? string.Empty}";
        }
    }

    public class GraphQLQueryGenerationContext
    {
        public string QueryVariableName { get; set; }


        public Dictionary<string, string> AvailableVariables { get; set; }

        public SemanticModel SemanticModel { get; set; }
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