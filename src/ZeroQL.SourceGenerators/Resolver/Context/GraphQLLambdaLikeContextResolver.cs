using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable CS8618

namespace ZeroQL.SourceGenerators.Resolver.Context;

public class UploadInfoByType
{
    public string SafeName { get; set; }

    public ITypeSymbol Type { get; set; }

    public IPropertySymbol[] UploadProperties { get; set; }
}

public class GraphQLLambdaLikeContextResolver
{
    public Error WrongMethod = new Error("Could not find query method");

    public Result<GraphQLSourceGenerationContext> Resolve(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var queryMethod = QueryAnalyzerHelper.ExtractQueryMethod(semanticModel.Compilation, invocation);
        if (queryMethod is null)
        {
            return WrongMethod;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new Error("Cancelled");
        }

        var graphqlLambda = invocation.ArgumentList.Arguments
            .Last(o => o.Expression is LambdaExpressionSyntax);
        var key = graphqlLambda.ToString();
        var nameColon = graphqlLambda.NameColon?.ToString();
        if (nameColon is not null)
        {
            key = key.Replace(nameColon, string.Empty).Trim();
        }

        var possibleGraphQLLambdaSymbol = semanticModel.GetSymbolInfo(graphqlLambda.Expression);
        if (possibleGraphQLLambdaSymbol.Symbol is not IMethodSymbol graphQLMethodSymbol)
        {
            return new Error("Could not find lambda symbol");
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return new Error("Could not find member access");
        }

        var queryType = graphQLMethodSymbol.Parameters.Last().Type;
        var operationKind = queryType.AllInterfaces.Any(o => o.Name == "IQuery")
            ? "query"
            : "mutation";

        var possibleMethodSymbol = semanticModel.GetSymbolInfo(invocation);
        if (possibleMethodSymbol.Symbol is not IMethodSymbol methodSymbol)
        {
            return new Error("Could not find method symbol");
        }

        var (name, nameError) = ResolveName(invocation, methodSymbol).Unwrap();
        if (nameError)
        {
            return nameError;
        }

        var (graphQLMethodInputType, executionStrategy) =
            GetInputSymbol(graphQLMethodSymbol, semanticModel.Compilation);
        if (graphQLMethodInputType is null)
        {
            return new Error("Could not find input type");
        }

        var requestExecutorInputArgumentSymbol = graphQLMethodInputType.IsAnonymousType
            ? graphQLMethodInputType.BaseType!
            : graphQLMethodInputType;

        if (cancellationToken.IsCancellationRequested)
        {
            return new Error("Cancelled");
        }

        var (result, error) = GraphQLQueryResolver
            .Resolve(semanticModel, graphqlLambda.Expression, cancellationToken)
            .Unwrap();

        if (error)
        {
            return error;
        }

        var (uploadType, uploadProperties) =
            FindAllUploadProperties(graphQLMethodInputType, result.Variables, semanticModel);
        var query = $"{operationKind} {name ?? string.Empty}{result.Query}";
        if (cancellationToken.IsCancellationRequested)
        {
            return new Error("Cancelled");
        }

        return new GraphQLSourceGenerationContext(
            key,
            executionStrategy,
            name,
            operationKind,
            query,
            result.Query,
            queryType.ToGlobalName(),
            graphQLMethodInputType,
            requestExecutorInputArgumentSymbol,
            uploadType,
            uploadProperties);
    }

    private Result<string?> ResolveName(InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol)
    {
        var hasName = methodSymbol.Parameters.First().Name == "name";
        if (hasName)
        {
            var expression = invocation.ArgumentList.Arguments.First().Expression;
            if (expression is not LiteralExpressionSyntax literal)
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.GraphQLQueryNameShouldBeLiteral,
                    expression.GetLocation());

                return new ErrorWithData<Diagnostic>(
                    "GraphQLQueryNameShouldBeLiteral",
                    diagnostic);
            }

            return literal.Token.ValueText;
        }

        return (string?)null;
    }

    public static (INamedTypeSymbol UploadType, UploadInfoByType[] UploadProperties) FindAllUploadProperties(
        INamedTypeSymbol inputType,
        Dictionary<string, GraphQLQueryVariable> variables,
        SemanticModel semanticModel)
    {
        var uploadType = semanticModel.Compilation.GetTypeByMetadataName("ZeroQL.Upload")!;
        var uploadPropertiesFromInputType = FindUploadPropertiesForType(inputType, uploadType, new HashSet<string>());
        var uploadPropertiesFromVariables = variables
            .Select(o => o.Value.TypeSymbol)
            .SelectMany(o => FindUploadPropertiesForType(o!, uploadType, new HashSet<string>()))
            .ToArray();

        var uploadProperties = uploadPropertiesFromInputType
            .Concat(uploadPropertiesFromVariables)
            .GroupBy(o => o.SafeName)
            .Select(o => o.First())
            .ToArray();

        return (uploadType, uploadProperties);
    }

    private static (INamedTypeSymbol? InputSymbol, GraphQLQueryExecutionStrategy ExecutionStrategy) GetInputSymbol(
        IMethodSymbol lambdaSymbol, Compilation compilation)
    {
        if (lambdaSymbol.Parameters.Length == 1)
        {
            return (GetLambdaEntry(compilation), GraphQLQueryExecutionStrategy.LambdaWithClosure);
        }

        return (lambdaSymbol.Parameters.First().GetNamedTypeSymbol(),
            GraphQLQueryExecutionStrategy.LambdaWithVariables);
    }

    private static INamedTypeSymbol GetLambdaEntry(Compilation compilation)
    {
        var @string = compilation.GetTypeByMetadataName("System.String")!;
        var @object = compilation.GetTypeByMetadataName("System.Object")!;
        var dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2")!
            .Construct(@string, @object);
        return dictionary;
    }

    public static ImmutableArray<UploadInfoByType> FindUploadPropertiesForType(
        ITypeSymbol inputType,
        INamedTypeSymbol upload,
        HashSet<string> processedTypes)
    {
        var inputTypeGlobalName = inputType.ToDisplayString();
        if (processedTypes.Contains(inputTypeGlobalName) || Utils.CSharpToGraphQL.ContainsKey(inputTypeGlobalName))
        {
            return ImmutableArray<UploadInfoByType>.Empty;
        }

        processedTypes.Add(inputTypeGlobalName);

        switch (inputType)
        {
            case INamedTypeSymbol namedTypeSymbol:
            {
                return HandleNamedType(namedTypeSymbol, upload, processedTypes);
            }
            case IArrayTypeSymbol arrayTypeSymbol:
            {
                var uploadPropertiesForArrayElement =
                    FindUploadPropertiesForType(arrayTypeSymbol.ElementType, upload, processedTypes);
                if (uploadPropertiesForArrayElement.Any())
                {
                    return uploadPropertiesForArrayElement.Add(new UploadInfoByType()
                    {
                        SafeName = arrayTypeSymbol.ToSafeGlobalName(),
                        Type = arrayTypeSymbol,
                        UploadProperties = Array.Empty<IPropertySymbol>()
                    });
                }

                return ImmutableArray<UploadInfoByType>.Empty;
            }
        }

        return ImmutableArray<UploadInfoByType>.Empty;
    }

    private static ImmutableArray<UploadInfoByType> HandleNamedType(INamedTypeSymbol inputType,
        INamedTypeSymbol upload,
        HashSet<string> processedTypes)
    {
        var properties = inputType
            .GetRealProperties()
            .Select(o =>
            {
                var isUpload = SymbolEqualityComparer.Default.Equals(o.Type, upload);
                return (Upload: isUpload, Property: o);
            })
            .ToArray();

        var uploadProperties = properties
            .Where(o => o.Upload)
            .Select(o => o.Property)
            .ToArray();

        var nonUploadProperties = properties
            .Where(o => !o.Upload)
            .Select(o => o.Property)
            .ToArray();

        var results = nonUploadProperties
            .Select(o => FindUploadPropertiesForType(o.Type, upload, processedTypes))
            .ToArray();

        var nodesToUpload = new List<int>();
        for (int i = 0; i < results.Length; i++)
        {
            var result = results[i];
            if (result.Any())
            {
                nodesToUpload.Add(i);
            }
        }

        if (nodesToUpload.Any())
        {
            return nodesToUpload
                .SelectMany(o => results[o])
                .ToImmutableArray()
                .Add(new UploadInfoByType()
                {
                    SafeName = inputType.ToSafeGlobalName(),
                    Type = inputType,
                    UploadProperties = nodesToUpload
                        .Select(o => nonUploadProperties[o])
                        .Concat(uploadProperties)
                        .ToArray()
                });
        }

        if (uploadProperties.Any())
        {
            return ImmutableArray.Create(new UploadInfoByType()
            {
                SafeName = inputType.ToSafeGlobalName(),
                Type = inputType,
                UploadProperties = nodesToUpload
                    .Select(o => nonUploadProperties[o])
                    .Concat(uploadProperties)
                    .ToArray()
            });
        }

        return ImmutableArray<UploadInfoByType>.Empty;
    }
}