using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.SourceGenerators.Resolver;

public class UploadInfoByType
{
    public ITypeSymbol Type { get; set; }

    public IPropertySymbol[] UploadProperties { get; set; }
}

public class GraphQLQueryContainerResolver
{
    public string Key { get; private set; }

    public INamedTypeSymbol UploadType { get; private set; }

    public string OperationKind { get; private set; }

    public string QueryBody { get; private set; }

    public IMethodSymbol GraphQLLambdaSymbol { get; private set; }

    public string RequestExecutorQueryType { get; private set; }

    public INamedTypeSymbol GraphQLInputSymbol { get; private set; }

    public INamedTypeSymbol RequestExecutorInputArgumentSymbol { get; private set; }

    public UploadInfoByType[] UploadPropeties { get; private set; }

    public Error? Resolve(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var queryMethod = QueryAnalyzerHelper.ExtractQueryMethod(semanticModel.Compilation, invocation);
        if (queryMethod is null)
        {
            return new Error("Could not find query method");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new Error("Cancelled");
        }

        var graphqlLambda = invocation.ArgumentList.Arguments.Last();
        var possibleGraphQLLambdaSymbol = semanticModel.GetSymbolInfo(graphqlLambda.Expression);

        Key = graphqlLambda.ToString();

        if (possibleGraphQLLambdaSymbol.Symbol is not IMethodSymbol lambdaSymbol)
        {
            return new Error("Could not find lambda symbol");
        }

        GraphQLLambdaSymbol = lambdaSymbol;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return new Error("Could not find member access");
        }

        OperationKind = memberAccess.Name.Identifier.Text.ToLower();

        GraphQLInputSymbol = GetInputSymbol(lambdaSymbol, semanticModel.Compilation);
        RequestExecutorInputArgumentSymbol = GraphQLInputSymbol.IsAnonymousType ? GraphQLInputSymbol.BaseType! : GraphQLInputSymbol;
        RequestExecutorQueryType = lambdaSymbol.Parameters.Last().ToGlobalName();

        if (cancellationToken.IsCancellationRequested)
        {
            return new Error("Cancelled");
        }

        FindAllUploadProperties(lambdaSymbol, semanticModel);

        var (query, error) = GraphQLQueryResolver.Resolve(semanticModel, graphqlLambda.Expression, cancellationToken).Unwrap();
        if (error)
        {
            return error;
        }

        QueryBody = query;

        if (cancellationToken.IsCancellationRequested)
        {
            return new Error("Cancelled");
        }

        return null;
    }


    private void FindAllUploadProperties(IMethodSymbol lambdaSymbol, SemanticModel semanticModel)
    {
        var inputType = lambdaSymbol.Parameters
            .First()
            .GetNamedTypeSymbol();

        UploadType = semanticModel.Compilation.GetTypeByMetadataName("ZeroQL.Upload")!;
        UploadPropeties = FindUploadPropertiesForType(inputType, UploadType, new HashSet<string>())
            .ToArray();
    }


    private static INamedTypeSymbol GetInputSymbol(IMethodSymbol lambdaSymbol, Compilation compilation)
    {
        if (lambdaSymbol.Parameters.Length == 1)
        {
            return compilation.GetTypeByMetadataName("ZeroQL.Core.Unit")!;
        }

        return lambdaSymbol.Parameters.First().GetNamedTypeSymbol();
    }

    public static UploadInfoByType[] FindUploadPropertiesForType(
        ITypeSymbol inputType,
        INamedTypeSymbol upload,
        HashSet<string> processedTypes)
    {
        var inputTypeGlobalName = inputType.ToDisplayString();
        if (processedTypes.Contains(inputTypeGlobalName) || Utils.CSharpToGraphQL.ContainsKey(inputTypeGlobalName))
        {
            return Array.Empty<UploadInfoByType>();
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
                return new[]
                    {
                        new UploadInfoByType()
                        {
                            Type = arrayTypeSymbol,
                            UploadProperties = Array.Empty<IPropertySymbol>()
                        }
                    }
                    .Concat(FindUploadPropertiesForType(arrayTypeSymbol.ElementType, upload, processedTypes))
                    .ToArray();
            }
        }

        return Array.Empty<UploadInfoByType>();
    }

    private static UploadInfoByType[] HandleNamedType(INamedTypeSymbol inputType, INamedTypeSymbol upload, HashSet<string> processedTypes)
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
            return new[]
                {
                    new UploadInfoByType()
                    {
                        Type = inputType,
                        UploadProperties = nodesToUpload
                            .Select(o => nonUploadProperties[o])
                            .Concat(uploadProperties)
                            .ToArray()
                    }
                }
                .Concat(nodesToUpload
                    .SelectMany(o => results[o])
                    .ToArray())
                .ToArray();
        }

        if (uploadProperties.Any())
        {
            return new[]
            {
                new UploadInfoByType
                {
                    Type = inputType,
                    UploadProperties = uploadProperties
                }
            };
        }

        return Array.Empty<UploadInfoByType>();
    }
}