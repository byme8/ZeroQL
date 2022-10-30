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
    public const string CANCELLED = nameof(CANCELLED);

    public static Result<string> Resolve(SemanticModel semanticModel, ExpressionSyntax query, CancellationToken cancellationToken)
    {
        if (query is not LambdaExpressionSyntax lambda)
        {
            return Failed(query);
        }

        var inputs = GetQueryInputs(lambda);
        var variables = GetVariablesFromLambda(semanticModel, lambda, cancellationToken);
        var availableVariables = inputs.VariablesName is null ? new Dictionary<string, string>()
            : variables
                .ToDictionary(
                    o => $"{inputs.VariablesName}.{o.Name}",
                    o => "$" + o.Name.FirstToLower());

        var context = new GraphQLResolveContext(
            inputs.QueryName,
            lambda,
            availableVariables,
            semanticModel,
            cancellationToken);

        var body = ResolveQuery(context, lambda.Body);
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

    public static Result<string> Resolve(SemanticModel semanticModel, MethodDeclarationSyntax method, CancellationToken cancellationToken)
    {
        var inputs = GetQueryInputs(method);
        var variables = GetVariablesFromRequestMethod(semanticModel, method, cancellationToken);
        var availableVariables = variables
            .ToDictionary(
                o => o.Name,
                o => "$" + o.Name.FirstToLower());

        var context = new GraphQLResolveContext(
            inputs.QueryName,
            method,
            availableVariables,
            semanticModel,
            cancellationToken);

        var body = ResolveQuery(context, method);
        if (body.Error)
        {
            return body;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        if (variables.Any())
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

    public static Result<string> ResolveFragmentTemplate(SemanticModel semanticModel, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
    {
        var parameters = methodDeclaration.ParameterList.Parameters;
        var queryName = parameters.First().Identifier.Text;
        var variables = parameters.Skip(1)
            .Select(o => o.Identifier.Text.FirstToLower())
            .ToArray();

        var availableVariables = !variables.Any() ? new Dictionary<string, string>()
            : variables
                .ToDictionary(
                    o => o,
                    o => "{{" + o + "}}");

        var context = new GraphQLResolveContext(
            queryName,
            methodDeclaration,
            availableVariables,
            semanticModel,
            cancellationToken);


        var (body, error) = HandleMethod(context, methodDeclaration, methodDeclaration).Unwrap();
        if (error)
        {
            return error;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return string.Empty;
        }

        return body;
    }

    private static (string? VariablesName, string QueryName) GetQueryInputs(CSharpSyntaxNode lambda)
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

        if (lambda is MethodDeclarationSyntax methodDeclaration)
        {
            var parameters = methodDeclaration.ParameterList.Parameters;
            return (null, parameters.First().Identifier.ValueText);
        }

        return default;
    }

    private static (string Name, string Type)[] GetVariablesFromLambda(SemanticModel semanticModel, LambdaExpressionSyntax lambda, CancellationToken cancellationToken)
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
            .Select(o => (o.Name, o.Type.ToGraphQLType()))
            .ToArray();
    }

    private static (string Name, string Type)[] GetVariablesFromRequestMethod(SemanticModel semanticModel, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
    {
        var method = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
        if (method!.ContainingSymbol is not INamedTypeSymbol containingType)
        {
            return Array.Empty<(string Name, string Type)>();
        }

        return containingType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(o => !o.IsImplicitlyDeclared)
            .Select(o => (o.Name, o.Type.ToGraphQLType()))
            .ToArray();
    }

    private static Result<string> ResolveQuery(GraphQLResolveContext context, CSharpSyntaxNode node)
    {
        if (context.CancellationToken.IsCancellationRequested)
        {
            return new Error(CANCELLED);
        }

        switch (node)
        {
            case InvocationExpressionSyntax invocation:
            {
                return HandleInvocation(context, invocation);
            }
            case MemberAccessExpressionSyntax member:
            {
                return HandleMemberAccess(context, member);
            }
            case IdentifierNameSyntax identifierNameSyntax:
            {
                return HanleIndentifier(context, node, identifierNameSyntax);
            }
            case SimpleLambdaExpressionSyntax simpleLambda:
            {
                return HandlerSimpleLambda(context, simpleLambda);
            }
            case ArgumentSyntax argument when context.Parent is ObjectCreationExpressionSyntax:
            {
                return HandleArgumentAsObjectCreation(context, argument);
            }
            case ArgumentSyntax argument:
            {
                return HandleArgumentAsVariable(context, argument);
            }
            case AnonymousObjectCreationExpressionSyntax anonymous:
            {
                return HandleAnonymousObjectCreation(context, anonymous);
            }
            case AnonymousObjectMemberDeclaratorSyntax anonymousMember:
            {
                return ResolveQuery(context.WithParent(anonymousMember), anonymousMember.Expression);
            }
            case ObjectCreationExpressionSyntax objectCreation:
            {
                return HandleObjectCreation(context, objectCreation);
            }
            case MethodDeclarationSyntax methodDeclaration:
            {
                return HandleMethod(context, methodDeclaration, methodDeclaration);
            }
            case PostfixUnaryExpressionSyntax postfixUnary:
            {
                return ResolveQuery(context.WithParent(postfixUnary), postfixUnary.Operand);
            }
        }

        return Failed(node);
    }

    private static Result<string> HandleObjectCreation(GraphQLResolveContext context, ObjectCreationExpressionSyntax objectCreation)
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
            .Select(o => ResolveQuery(context.WithParent(objectCreation), o))
            .ToArray();

        if (results.Any(o => o.Error))
        {
            return results.First().Error!;
        }

        return results.Select(o => o.Value!).Join(" ");
    }

    private static Result<string> HandleArgumentAsObjectCreation(GraphQLResolveContext context, ArgumentSyntax argument)
    {
        return ResolveQuery(context.WithParent(argument), argument.Expression);
    }

    private static Result<string> HandleAnonymousObjectCreation(GraphQLResolveContext context, AnonymousObjectCreationExpressionSyntax anonymous)
    {
        var initializers = new List<string>(anonymous.Initializers.Count);
        foreach (var initializer in anonymous.Initializers)
        {
            var (result, error) = ResolveQuery(context, initializer).Unwrap();
            if (error)
            {
                return error;
            }

            initializers.Add(result);
        }

        return initializers.Join(" ");
    }

    private static Result<string> HandleArgumentAsVariable(GraphQLResolveContext context, ArgumentSyntax argument)
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

        if (argument.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken);
            var namedType = symbol.GetTypeSymbol();
            if (namedType is null)
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

    private static Result<string> HandlerSimpleLambda(GraphQLResolveContext context, SimpleLambdaExpressionSyntax simpleLambda)
    {
        if (QueryAnalyzerHelper.IsOpenLambda(simpleLambda))
        {
            return Failed(simpleLambda);
        }

        var parameter = simpleLambda.Parameter.Identifier.ValueText;
        var childContext = context with { QueryVariableName = parameter };

        return ResolveQuery(childContext.WithParent(simpleLambda), simpleLambda.Body);
    }

    private static Result<string> HanleIndentifier(GraphQLResolveContext context, CSharpSyntaxNode node, IdentifierNameSyntax identifierNameSyntax)
    {
        if (identifierNameSyntax.Identifier.ValueText == context.QueryVariableName)
        {
            return string.Empty;
        }

        return Failed(node);
    }

    private static Result<string> HandleMemberAccess(GraphQLResolveContext context, MemberAccessExpressionSyntax member)
    {
        if (member.Expression is MemberAccessExpressionSyntax left)
        {
            return ResolveQuery(context.WithParent(member), left);
        }

        if (member.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == context.QueryVariableName)
        {
            return member.Name.Identifier.ValueText.FirstToLower();
        }

        if (member.Expression is PostfixUnaryExpressionSyntax { Operand: IdentifierNameSyntax postfixIdentifier } &&
            postfixIdentifier.Identifier.ValueText == context.QueryVariableName)
        {
            return member.Name.Identifier.ValueText.FirstToLower();
        }

        return Failed(member.Expression);
    }

    private static Result<string> HandleInvocation(GraphQLResolveContext context, InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax identifierName } &&
            identifierName.Identifier.ValueText != context.QueryVariableName)
        {
            return Failed(identifierName, Descriptors.DontUseOutScopeValues);
        }

        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbol.Symbol is not IMethodSymbol method)
        {
            return Failed(invocation);
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return new Error(CANCELLED);
        }

        var attributes = method
            .GetAttributes();

        var hasFieldSelector = attributes
            .Any(o => SymbolEqualityComparer.Default.Equals(o.AttributeClass, context.FieldSelectorAttribute));

        var hasFragment = attributes
            .Any(o => SymbolEqualityComparer.Default.Equals(o.AttributeClass, context.FragmentAttribute));

        var hasFragmentTemplate = attributes
            .Any(o => SymbolEqualityComparer.Default.Equals(o.AttributeClass, context.TemplateAttribute));

        if (!hasFieldSelector && !hasFragment && !hasFragmentTemplate)
        {
            return Failed(invocation, Descriptors.OnlyFieldSelectorsAndFragmentsAreAllowed);
        }

        if (hasFragmentTemplate)
        {
            return HandleFragmentWithoutSyntaxTree(context, invocation, method);
        }

        if (hasFieldSelector)
        {
            return HandleFieldSelector(context, invocation, method);
        }

        return HandleFragment(context, invocation, method);

    }

    private static Result<string> HandleFragment(GraphQLResolveContext context, InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
        if (method.DeclaringSyntaxReferences.IsEmpty)
        {
            return HandleFragmentWithoutSyntaxTree(context, invocation, method);
        }

        var fragment = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (fragment is not MethodDeclarationSyntax methodDeclaration)
        {
            return Failed(invocation);
        }

        var currentCompilation = context.SemanticModel.Compilation;
        var compilation = GetCompilation(fragment.SyntaxTree, currentCompilation);
        if (compilation.Error || currentCompilation != compilation.Value)
        {
            return Failed(invocation, Descriptors.FragmentsWithoutSyntaxTree);
        }


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
            return new Error(CANCELLED);
        }

        var newSemanticModel = compilation.Value.GetSemanticModel(fragment.SyntaxTree);
        context = context.WithVariableName(name) with
        {
            SemanticModel = newSemanticModel,
            AvailableVariables = variables.ToDictionary(o => o.Key, o => o.Value.Value!)
        };

        return HandleMethod(context, invocation, methodDeclaration);
    }

    private static Result<string> HandleMethod(GraphQLResolveContext context, CSharpSyntaxNode node, MethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration.Body is not null)
        {
            if (methodDeclaration.Body.Statements.First() is ReturnStatementSyntax { Expression: { } } returnStatement)
            {
                return ResolveQuery(context.WithParent(returnStatement), returnStatement.Expression);
            }

            return Failed(methodDeclaration.Body.Statements.First());
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return new Error(CANCELLED);
        }

        if (methodDeclaration.ExpressionBody is not null)
        {
            return ResolveQuery(context.WithParent(methodDeclaration.ExpressionBody), methodDeclaration.ExpressionBody.Expression);
        }

        return Failed(node);
    }

    private static Result<string> HandleFragmentWithoutSyntaxTree(GraphQLResolveContext context, InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
        var graphQLQueryTemplate = method
            .GetAttributes()
            .FirstOrDefault(o => SymbolEqualityComparer.Default.Equals(o.AttributeClass, context.TemplateAttribute))?
            .ConstructorArguments
            .FirstOrDefault()
            .Value?
            .ToString();

        if (graphQLQueryTemplate is null)
        {
            return Failed(invocation);
        }

        var parameters = method.Parameters;
        var variablesToMap = parameters
            .Select(o => o.Name)
            .ToArray();

        var inputVariables = invocation.ArgumentList.Arguments;
        var fragmentVariablesToInputVariables = inputVariables
            .Select((o, i) => (Key: variablesToMap[i], Value: HandleArgumentAsVariable(context, o)))
            .ToArray();

        for (int i = 0; i < fragmentVariablesToInputVariables.Length; i++)
        {
            var variable = fragmentVariablesToInputVariables[i];
            if (variable.Value.Error)
            {
                return Failed(invocation.ArgumentList.Arguments[i]);
            }
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return new Error(CANCELLED);
        }

        var finalGraphQLQuery = fragmentVariablesToInputVariables
            .Aggregate(
                graphQLQueryTemplate,
                (current, variable) => current.Replace($"{{{{{variable.Key}}}}}", variable.Value.Value));

        return finalGraphQLQuery;
    }

    private static Result<string> HandleFieldSelector(GraphQLResolveContext context, InvocationExpressionSyntax invocation, IMethodSymbol method)
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
                .Select(o => ResolveQuery(context.WithParent(o), o))
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
            var body = ResolveQuery(context.WithParent(invocation), invocation.ArgumentList.Arguments.Last().Expression);
            if (body.Error)
            {
                return body;
            }
            stringBuilder.Append($" {{ {body.Value} }} ");
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
            descriptor ?? Descriptors.FailedToConvertPartOfTheQuery,
            node.GetLocation(),
            node.ToString());

        return new ErrorWithData<Diagnostic>("Failed", diagnostic);
    }
}