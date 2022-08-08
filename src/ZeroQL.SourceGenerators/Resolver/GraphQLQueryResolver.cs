using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.SourceGenerators.Resolver;

public class GraphQLQueryResolver
{
    public const string Cancelled = nameof(Cancelled);

    public static Result<string> Resolve(SemanticModel semanticModel, ExpressionSyntax query, CancellationToken cancellationToken)
    {
        if (query is not LambdaExpressionSyntax lambda)
        {
            return Failed(query);
        }

        var inputs = GetQueryInputs(lambda);
        var variables = GetVariables(semanticModel, lambda, cancellationToken);
        var availableVariables = inputs.VariablesName is null ? new Dictionary<string, string>()
            : variables
                .ToDictionary(
                    o => $"{inputs.VariablesName}.{o.Name}",
                    o => "$" + o.Name.FirstToLower());

        var generationContext = new GraphQLResolveContext(
            inputs.QueryName,
            lambda,
            availableVariables,
            semanticModel,
            cancellationToken);

        var body = ResolveQuery(generationContext, lambda.Body);
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

    private static (string Name, string Type)[] GetVariables(SemanticModel semanticModel, LambdaExpressionSyntax lambda, CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetSymbolInfo(lambda, cancellationToken);
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

    private static Result<string> ResolveQuery(GraphQLResolveContext generationContext, CSharpSyntaxNode node)
    {
        if (generationContext.CancellationToken.IsCancellationRequested)
        {
            return new Error(Cancelled);
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
                return ResolveQuery(generationContext.WithParent(anonymousMember), anonymousMember.Expression);
            }
            case ObjectCreationExpressionSyntax objectCreation:
            {
                return HandleObjectCreation(generationContext, objectCreation);
            }
        }

        return Failed(node);
    }

    private static Result<string> HandleObjectCreation(GraphQLResolveContext generationContext, ObjectCreationExpressionSyntax objectCreation)
    {
        var arguments = objectCreation.ArgumentList?.Arguments.ToArray();
        var initializers = objectCreation.Initializer?.Expressions
            .OfType<AssignmentExpressionSyntax>()
            .Select(o => o.Right)
            .ToArray();

        if (arguments is null && initializers is null)
        {
            return string.Empty;
        }

        var expressions = new List<CSharpSyntaxNode>();
        if (arguments is not null)
        {
            expressions.AddRange(arguments);
        }

        if (initializers is not null)
        {
            expressions.AddRange(initializers);
        }

        var results = expressions
            .Select(o => ResolveQuery(generationContext.WithParent(objectCreation), o))
            .ToArray();

        if (results.Any(o => o.Error))
        {
            return results.First().Error;
        }

        return results.Select(o => o.Value).Join(" ");
    }

    private static Result<string> HandleArgumentAsObjectCreation(GraphQLResolveContext generationContext, ArgumentSyntax argument)
    {
        return ResolveQuery(generationContext.WithParent(argument), argument.Expression);
    }

    private static Result<string> HandleAnonymousObjectCreation(GraphQLResolveContext generationContext, AnonymousObjectCreationExpressionSyntax anonymous)
    {
        var initializers = new List<string>(anonymous.Initializers.Count);
        foreach (var initializer in anonymous.Initializers)
        {
            var result = ResolveQuery(generationContext, initializer);
            if (result.Error)
            {
                return result.Error;
            }

            initializers.Add(result.Value);
        }

        return initializers.Join(" ");
    }

    private static Result<string> HandleArgumentAsVariable(GraphQLResolveContext generationContext, ArgumentSyntax argument)
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
            var symbol = generationContext.SemanticModel.GetSymbolInfo(memberAccess.Expression, generationContext.CancellationToken);
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

    private static Result<string> HandlerSimpleLambda(GraphQLResolveContext generationContext, SimpleLambdaExpressionSyntax simpleLambda)
    {
        if (QueryAnalyzerHelper.IsOpenLambda(simpleLambda))
        {
            return Failed(simpleLambda);
        }

        var parameter = simpleLambda.Parameter.Identifier.ValueText;
        var childGenerationContext = generationContext with { QueryVariableName = parameter };

        return ResolveQuery(childGenerationContext.WithParent(simpleLambda), simpleLambda.Body);
    }

    private static Result<string> HanleIndentifier(GraphQLResolveContext generationContext, CSharpSyntaxNode node, IdentifierNameSyntax identifierNameSyntax)
    {
        if (identifierNameSyntax.Identifier.ValueText == generationContext.QueryVariableName)
        {
            return string.Empty;
        }

        return Failed(node);
    }

    private static Result<string> HandleMemberAccess(GraphQLResolveContext generationContext, MemberAccessExpressionSyntax member)
    {
        if (member.Expression is MemberAccessExpressionSyntax left)
        {
            return ResolveQuery(generationContext.WithParent(member), left);
        }

        if (member.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == generationContext.QueryVariableName)
        {
            return member.Name.Identifier.ValueText.FirstToLower();
        }

        return Failed(member.Expression);
    }

    private static Result<string> HandleInvocation(GraphQLResolveContext generationContext, InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax identifierName } &&
            identifierName.Identifier.ValueText != generationContext.QueryVariableName)
        {
            return Failed(identifierName, Descriptors.DontUserOutScopeValues);
        }

        var symbol = generationContext.SemanticModel.GetSymbolInfo(invocation, generationContext.CancellationToken);
        if (symbol.Symbol is not IMethodSymbol method)
        {
            return Failed(invocation);
        }

        if (generationContext.CancellationToken.IsCancellationRequested)
        {
            return new Error(Cancelled);
        }

        var attributes = method
            .GetAttributes();

        var hasFieldSelector = attributes
            .Any(o => SymbolEqualityComparer.Default.Equals(o.AttributeClass, generationContext.FieldSelectorAttribute));

        var hasFragment = attributes
            .Any(o => SymbolEqualityComparer.Default.Equals(o.AttributeClass, generationContext.FragmentAttribute));

        if (!hasFieldSelector && !hasFragment)
        {
            return Failed(invocation, Descriptors.OnlyFieldSelectorsAndFragmentsAreAllowed);
        }

        if (hasFieldSelector)
        {
            return HandleFieldSelector(generationContext, invocation, method);
        }

        return HandleFragment(generationContext, invocation, method);

    }

    private static Result<string> HandleFragment(GraphQLResolveContext generationContext, InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
        if (method.DeclaringSyntaxReferences.IsEmpty)
        {
            var methodHasTemplate = method
                .GetAttributes()
                .Any(o => o.AttributeClass == generationContext.FragmentQueryAttribute);

            if (!methodHasTemplate)
            {
                return new Error("Method without template");
            }

            var result = HandleFragmentWithoutSyntaxTree(generationContext, invocation, method);
            if (result.Error)
            {
                return new Error("Failed Fragment with out Syntax Tree");
            }

            return result;
        }

        var fragment = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (fragment is not MethodDeclarationSyntax methodDeclaration)
        {
            return Failed(invocation);
        }

        var currentCompilation = generationContext.SemanticModel.Compilation;
        var compilation = GetCompilation(fragment.SyntaxTree, currentCompilation);
        if (compilation.Error || currentCompilation != compilation.Value)
        {
            return Failed(invocation, Descriptors.FragmentsWithoutSyntaxTree);
        }

        var newSemanticModel = compilation.Value.GetSemanticModel(fragment.SyntaxTree);
        generationContext = generationContext with { SemanticModel = newSemanticModel };

        var parameters = methodDeclaration.ParameterList.Parameters;
        var name = parameters.FirstOrDefault()?.Identifier.Text;
        if (name is null)
        {
            return Failed(invocation);
        }

        var variablesToMap = parameters
            .Skip(1)
            .Select(o => o.Identifier.Text)
            .ToArray();

        var inputVariables = invocation.ArgumentList.Arguments;

        var variables = inputVariables
            .Select((o, i) => (Key: variablesToMap[i], Value: HandleArgumentAsVariable(generationContext, o)))
            .ToArray();

        for (int i = 0; i < variables.Length; i++)
        {
            var variable = variables[i];
            if (variable.Value.Error)
            {
                return Failed(invocation.ArgumentList.Arguments[i]);
            }
        }

        if (generationContext.CancellationToken.IsCancellationRequested)
        {
            return new Error(Cancelled);
        }

        var newContext = generationContext.WithVariableName(name) with
        {
            AvailableVariables = variables.ToDictionary(o => o.Key, o => o.Value.Value)
        };

        if (methodDeclaration.Body is not null)
        {
            if (methodDeclaration.Body.Statements.First() is ReturnStatementSyntax { Expression: { } } returnStatement)
            {
                return ResolveQuery(newContext.WithParent(returnStatement), returnStatement.Expression);
            }

            return Failed(methodDeclaration.Body.Statements.First());
        }

        if (generationContext.CancellationToken.IsCancellationRequested)
        {
            return new Error(Cancelled);
        }

        if (methodDeclaration.ExpressionBody is not null)
        {
            return ResolveQuery(newContext.WithParent(methodDeclaration.ExpressionBody), methodDeclaration.ExpressionBody.Expression);
        }

        return Failed(invocation);
    }

    private static Result<string> HandleFragmentWithoutSyntaxTree(GraphQLResolveContext context, InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
        var graphQLQueryTemplate = method
            .GetAttributes()
            .FirstOrDefault(o => SymbolEqualityComparer.Default.Equals(o.AttributeClass, context.FragmentQueryAttribute))?
            .ConstructorArguments
            .FirstOrDefault()
            .Value?
            .ToString();

        if (graphQLQueryTemplate is null)
        {
            return new Error("Fragment query is not found");
        }

        var parameters = method.Parameters;
        var variablesToMap = parameters
            .Select(o => o.Name)
            .ToArray();

        var inputVariables = invocation.ArgumentList.Arguments;
        var variables = inputVariables
            .Select((o, i) => (Key: variablesToMap[i], Value: HandleArgumentAsVariable(context, o)))
            .ToArray();

        for (int i = 0; i < variables.Length; i++)
        {
            var variable = variables[i];
            if (variable.Value.Error)
            {
                return Failed(invocation.ArgumentList.Arguments[i]);
            }
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return new Error(Cancelled);
        }

        var finalGraphQLQuery = variables
            .Aggregate(
                graphQLQueryTemplate,
                (current, variable) => current.Replace($"{{{{{variable.Key}}}}}", variable.Value.Value));

        return finalGraphQLQuery;
    }

    private static Result<string> HandleFieldSelector(GraphQLResolveContext generationContext, InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
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
                .Select(o => ResolveQuery(generationContext.WithParent(o), o))
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
            var generateBody = ResolveQuery(generationContext.WithParent(invocation), invocation.ArgumentList.Arguments.Last().Expression);
            if (generateBody.Error)
            {
                return generateBody;
            }
            stringBuilder.Append($" {{ {generateBody.Value} }} ");
        }

        return stringBuilder.ToString();
    }

    private static Result<Compilation> GetCompilation(SyntaxTree syntaxTree, Compilation compilation)
    {
        if (!compilation.ContainsSyntaxTree(syntaxTree))
        {
            var newCompilation = compilation.References
                .OfType<CompilationReference>()
                .FirstOrDefault(o => o.Compilation.ContainsSyntaxTree(syntaxTree))
                ?.Compilation;

            if (newCompilation is null)
            {
                return new Error("Failed to find compilation");
            }

            return newCompilation;
        }

        return compilation;
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