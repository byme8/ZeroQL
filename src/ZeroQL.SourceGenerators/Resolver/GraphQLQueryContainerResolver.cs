using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.SourceGenerators.Resolver;

public class GraphQLQueryContainerResolver
{
    public string Key { get; private set; }

    public string OperationKind { get; private set; }

    public string QueryBody { get; private set; }

    public IMethodSymbol GraphQLLambdaSymbol { get; private set; }

    public string RequestExecutorQueryType { get; private set; }

    public INamedTypeSymbol GraphQLInputSymbol { get; private set; }

    public INamedTypeSymbol RequestExecutorInputArgumentSymbol { get; private set; }

    public (INamedTypeSymbol Type, IPropertySymbol[] UploadProperties)[] UploadPropeties { get; private set; }

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

        GraphQLInputSymbol = GetVariableTypeName(lambdaSymbol, semanticModel.Compilation);
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

        var upload = semanticModel.Compilation.GetTypeByMetadataName("ZeroQL.Upload");
        UploadPropeties = FindUploadPropertiesForType(inputType, upload, new HashSet<string>())
            .ToArray();
    }

    private static INamedTypeSymbol GetVariableTypeName(IMethodSymbol lambdaSymbol, Compilation compilation)
    {
        if (lambdaSymbol.Parameters.Length == 1)
        {
            return compilation.GetTypeByMetadataName("ZeroQL.Core.Unit")!;
        }

        return lambdaSymbol.Parameters.First().GetNamedTypeSymbol();
    }

    public static IEnumerable<(INamedTypeSymbol Type, IPropertySymbol[] UploadProperties)> FindUploadPropertiesForType(
        INamedTypeSymbol inputType,
        INamedTypeSymbol? upload,
        HashSet<string> processedTypes)
    {
        if (processedTypes.Contains(inputType.ToGlobalName()))
        {
            yield break;
        }

        processedTypes.Add(inputType.ToGlobalName());

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

        yield return (inputType, uploadProperties);

        foreach (var nonUploadProperty in nonUploadProperties)
        {
            if (nonUploadProperty.Type is not INamedTypeSymbol namedType)
            {
                continue;
            }

            foreach (var item in FindUploadPropertiesForType(namedType, upload, processedTypes))
            {
                yield return (item.Type, item.UploadProperties);
            }
        }
    }
}