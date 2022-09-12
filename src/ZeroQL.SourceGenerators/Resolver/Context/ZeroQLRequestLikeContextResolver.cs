using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.SourceGenerators.Resolver.Context;

public class ZeroQLRequestLikeContextResolver
{
    public Result<GraphQLSourceGenerationContext> Resolve(RecordDeclarationSyntax record, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var key = record.Identifier.Text;
        var possibleRecordType = semanticModel.GetDeclaredSymbol(record);
        if (possibleRecordType is not INamedTypeSymbol recordType)
        {
            return new Error("Could not resolve record type");
        }

        if (record.BaseList!.Types.First().Type is not GenericNameSyntax genericName)
        {
            return new Error("The base type must be a generic type");
        }
        var possibleOperationType = semanticModel.GetTypeInfo(genericName.TypeArgumentList.Arguments.First());
        if (possibleOperationType.Type is not INamedTypeSymbol operationTypeSymbol)
        {
            return new Error("The operation type must be a named type");
        }

        var queryTypeName = operationTypeSymbol.ToGlobalName();
        var queryOperationKind = semanticModel.Compilation.GetTypeByMetadataName("ZeroQL.Internal.IQuery");
        var mutationOperationKind = semanticModel.Compilation.GetTypeByMetadataName("ZeroQL.Internal.IMutation");

        var operationName = record.Identifier.ValueText;
        var (operationType, error) = GetOperationType(operationTypeSymbol.Interfaces.First(), queryOperationKind, mutationOperationKind).Unwrap();
        if (error)
        {
            return error;
        }
        
        var executeMethod = record.Members
            .OfType<MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == "Execute");
        
        var (uploadType, uploadProperties) = GraphQLLambdaLikeContextResolver.FindAllUploadProperties(recordType, semanticModel);
        var (queryBody, queryError) = GraphQLQueryResolver.Resolve(semanticModel, executeMethod, cancellationToken).Unwrap();
        if (queryError)
        {
            return error;
        }

        var operationQuery = $"{operationType.ToLower()} {operationName}{queryBody}";
        var operationHash = GraphQLLambdaLikeContextResolver.ComputeHash(operationQuery);

        return new GraphQLSourceGenerationContext(
            key, 
            operationName,
            operationType,
            operationQuery,
            operationHash,
            queryTypeName,
            recordType,
            recordType,
            uploadType,
            uploadProperties
        );
    }

    private static Result<string> GetOperationType(
        INamedTypeSymbol operationType,
        INamedTypeSymbol? queryOperationKind,
        INamedTypeSymbol? mutationOperationKind)
    {
        if (SymbolEqualityComparer.Default.Equals(queryOperationKind, operationType))
        {
            return "Query";
        }

        if (SymbolEqualityComparer.Default.Equals(mutationOperationKind, operationType))
        {
            return "Mutation";
        }

        return new Error("The operation type must be a query or a mutation");
    }
}