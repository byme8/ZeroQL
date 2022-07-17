using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.SourceGenerators.Generator;

public class GraphQLQueryGenerator
{
    public static Result<string> Generate(SemanticModel semanticModel, ExpressionSyntax query, CancellationToken cancellationToken)
    {
        if (query is not LambdaExpressionSyntax lambda)
        {
            return Failed(query);
        }

        var inputs = GetQueryInputs(lambda);
        var variables = GetVariables(semanticModel, lambda);
        var availableVariables = inputs.VariablesName is null ? new Dictionary<string, string>()
            : variables
                .ToDictionary(
                    o => $"{inputs.VariablesName}.{o.Name}",
                    o => "$" + o.Name.FirstToLower());

        var generationContext = new GraphQLQueryGenerationContext(
            inputs.QueryName,
            lambda,
            availableVariables,
            semanticModel,
            cancellationToken);

        var body = GenerateQuery(generationContext, lambda.Body);
        if (body.Error)
        {
            return body;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        if (inputs.VariablesName != null)
        {
            var variablesBody = variables
                .Select(o => $"${o.Name.FirstToLower()}: {o.Type}")
                .Join()
                .Wrap("(", ") ");

            stringBuilder.Append(variablesBody);
        }
        stringBuilder.Append("{ ");
        stringBuilder.Append(body.Value);
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

    private static (string Name, string Type)[] GetVariables(SemanticModel semanticModel, LambdaExpressionSyntax lambda)
    {
        var symbol = semanticModel.GetSymbolInfo(lambda);
        if (symbol.Symbol is not IMethodSymbol method)
        {
            Failed(lambda);
            return Array.Empty<(string Name, string Type)>();
        }

        if (method.Parameters.Length == 1)
        {
            return Array.Empty<(string Name, string Type)>();
        }

        var type = method.Parameters.First().Type;

        return type.GetMembers()
            .OfType<IPropertySymbol>()
            .Select(o => (o.Name, o.Type.ToStringWithNullable()))
            .ToArray();
    }

    private static Result<string> GenerateQuery(GraphQLQueryGenerationContext generationContext, CSharpSyntaxNode node)
    {
        if (generationContext.CancellationToken.IsCancellationRequested)
        {
            return new Error("Cancelled");
        }

        switch (node)
        {
            case InvocationExpressionSyntax invocation:
            {
                return HandleInvocation(generationContext, invocation);
            }
            case MemberAccessExpressionSyntax member:
            {
                return HandleMemberAccess(generationContext, member);
            }
            case IdentifierNameSyntax identifierNameSyntax:
            {
                return HanleIndentifier(generationContext, node, identifierNameSyntax);
            }
            case SimpleLambdaExpressionSyntax simpleLambda:
            {
                return HandlerSimpleLambda(generationContext, simpleLambda);
            }
            case ArgumentSyntax argument when generationContext.Parent is ObjectCreationExpressionSyntax:
            {
                return HandleArgumentAsObjectCreation(generationContext, argument);
            }
            case ArgumentSyntax argument:
            {
                return HandleArgumentAsVariable(generationContext, argument);
            }
            case AnonymousObjectCreationExpressionSyntax anonymous:
            {
                return HandleAnonymousObjectCreation(generationContext, anonymous);
            }
            case AnonymousObjectMemberDeclaratorSyntax anonymousMember:
            {
                return GenerateQuery(generationContext.WithParent(anonymousMember), anonymousMember.Expression);
            }
            case ObjectCreationExpressionSyntax objectCreation:
            {
                return HandleObjectCreation(generationContext, objectCreation);
            }
        }

        return Failed(node);
    }

    private static Result<string> HandleObjectCreation(GraphQLQueryGenerationContext generationContext, ObjectCreationExpressionSyntax objectCreation)
    {
        var arguments = objectCreation.ArgumentList?.Arguments.ToArray();
        if (arguments is null)
        {
            return string.Empty;
        }

        var results = arguments
            .Select(o => GenerateQuery(generationContext.WithParent(objectCreation), o))
            .ToArray();

        if (results.Any(o => o.Error))
        {
            return results.First().Error;
        }

        return results.Select(o => o.Value).Join(" ");
    }

    private static Result<string> HandleArgumentAsObjectCreation(GraphQLQueryGenerationContext generationContext, ArgumentSyntax argument)
    {
        return GenerateQuery(generationContext.WithParent(argument), argument.Expression);
    }

    private static Result<string> HandleAnonymousObjectCreation(GraphQLQueryGenerationContext generationContext, AnonymousObjectCreationExpressionSyntax anonymous)
    {
        var initializers = new List<string>(anonymous.Initializers.Count);
        foreach (var initializer in anonymous.Initializers)
        {
            var result = GenerateQuery(generationContext, initializer);
            if (result.Error)
            {
                return result.Error;
            }

            initializers.Add(result.Value);
        }

        return initializers.Join(" ");
    }

    private static Result<string> HandleArgumentAsVariable(GraphQLQueryGenerationContext generationContext, ArgumentSyntax argument)
    {
        if (argument.Expression is LiteralExpressionSyntax literal)
        {
            return literal.ToString();
        }

        var value = argument.Expression.ToString();
        if (generationContext.AvailableVariables.ContainsKey(value))
        {
            return generationContext.AvailableVariables[value];
        }

        if (argument.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var symbol = generationContext.SemanticModel.GetSymbolInfo(memberAccess.Expression);
            if (symbol.Symbol is not INamedTypeSymbol namedType)
            {
                return Failed(memberAccess);
            }

            if (namedType.EnumUnderlyingType != null)
            {
                return memberAccess.Name.Identifier.Text.ToUpperCase();
            }
        }

        return Failed(argument);
    }

    private static Result<string> HandlerSimpleLambda(GraphQLQueryGenerationContext generationContext, SimpleLambdaExpressionSyntax simpleLambda)
    {
        if (QueryAnalyzerHelper.IsOpenLambda(simpleLambda))
        {
            return Failed(simpleLambda);
        }

        var parameter = simpleLambda.Parameter.Identifier.ValueText;
        var childGenerationContext = generationContext with { QueryVariableName = parameter };

        return GenerateQuery(childGenerationContext.WithParent(simpleLambda), simpleLambda.Body);
    }

    private static Result<string> HanleIndentifier(GraphQLQueryGenerationContext generationContext, CSharpSyntaxNode node, IdentifierNameSyntax identifierNameSyntax)
    {
        if (identifierNameSyntax.Identifier.ValueText == generationContext.QueryVariableName)
        {
            return string.Empty;
        }

        return Failed(node);
    }

    private static Result<string> HandleMemberAccess(GraphQLQueryGenerationContext generationContext, MemberAccessExpressionSyntax member)
    {
        if (member.Expression is MemberAccessExpressionSyntax left)
        {
            return GenerateQuery(generationContext.WithParent(member), left);
        }

        if (member.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == generationContext.QueryVariableName)
        {
            return member.Name.Identifier.ValueText.FirstToLower();
        }

        return Failed(member.Expression);
    }

    private static Result<string> HandleInvocation(GraphQLQueryGenerationContext generationContext, InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax identifierName } &&
            identifierName.Identifier.ValueText != generationContext.QueryVariableName)
        {
            return Failed(identifierName, Descriptors.DontUserOutScopeValues);
        }

        var symbol = generationContext.SemanticModel.GetSymbolInfo(invocation);
        if (symbol.Symbol is not IMethodSymbol method)
        {
            return Failed(invocation);
        }

        var haveFieldSelector = method
            .GetAttributes()
            .Any(o => o.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == SourceGeneratorInfo.GraphQLFieldSelectorAttribute);
        
        if (!haveFieldSelector)
        {
            return Failed(invocation, Descriptors.OnlyFieldSelectorsAreAllowed);
        }

        var ignoreLastParameter = method.Parameters.Last().Type.Name.StartsWith("Func");
        var parametersToIgnore = ignoreLastParameter ? 1 : 0;
        var argumentNames = method.Parameters
            .Take(method.Parameters.Length - parametersToIgnore)
            .Select(o => $"{o.Name.FirstToLower()}: ")
            .ToArray();

        var stringBuilder = new StringBuilder();
        stringBuilder.Append(method.Name.FirstToLower());
        if (argumentNames.Any())
        {
            var graphQLArguments = invocation.ArgumentList.Arguments
                .Take(argumentNames.Length)
                .Select(o => GenerateQuery(generationContext.WithParent(o), o))
                .ToArray();

            if (graphQLArguments.Any(o => o.Error))
            {
                return graphQLArguments.First(o => o.Error);
            }

            var formattedArguments = graphQLArguments
                .Select((o, i) => $"{argumentNames[i]}{o.Value}")
                .Join()
                .Wrap("(", ")");

            stringBuilder.Append(formattedArguments);
        }
        if (ignoreLastParameter)
        {
            var generateBody = GenerateQuery(generationContext.WithParent(invocation), invocation.ArgumentList.Arguments.Last().Expression);
            if (generateBody.Error)
            {
                return generateBody;
            }
            stringBuilder.Append($" {{ {generateBody.Value} }} ");
        }

        return stringBuilder.ToString();
    }

    private static Error Failed(CSharpSyntaxNode node, DiagnosticDescriptor? descriptor = null)
    {
        var diagnostic = Diagnostic.Create(
            descriptor ?? Descriptors.FailedToConvert,
            node.GetLocation(),
            node.ToString());

        return new ErrorWithData<Diagnostic>("Failed", diagnostic);
    }
}