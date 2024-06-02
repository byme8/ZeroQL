using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using ZeroQL.SourceGenerators.Resolver.Context;

namespace ZeroQL.SourceGenerators.Resolver;

public class GraphQLUploadResolver
{
    public static string GenerateUploadsSelectors(GraphQLQueryExecutionStrategy executionStrategy, UploadInfoByType[] types, INamedTypeSymbol uploadType)
    {
        if (types.Empty())
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var type in types.Where(o => o.Type is not IArrayTypeSymbol))
        {
            var name = GenerateTypeName(type.Type);
            sb.AppendLine($$"""

                    private static void Process_{{type.SafeName}}(MultipartFormDataContentContext context, {{name}} value, string path)
                    {
                        if (value == null)
                        {
                            return;
                        }
                        {{type.UploadProperties.Select(o => GenerateAccessor(o, uploadType)).JoinWithNewLine()}}
                    }
            """);

        }

        if (executionStrategy == GraphQLQueryExecutionStrategy.LambdaWithClosure)
        {
            sb.AppendLine("""

                    private static void Process(MultipartFormDataContentContext context, global::System.Collections.Generic.Dictionary<string, object> dictionary, string path)
                    {
                        foreach(var (key, value) in dictionary)
                        {
                            switch(value)
                            {
            """);

            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type.Type is IArrayTypeSymbol arrayTypeSymbol)
                {
                    sb.AppendLine($$"""
                                case {{arrayTypeSymbol.ToGlobalName()}} {{type.SafeName}}:
                                    for(int i = 0; i < {{type.SafeName}}.Length; i++)
                                    {
                                        Process_{{arrayTypeSymbol.ElementType.ToSafeGlobalName()}}(context, {{type.SafeName}}[i], $"{path}.{key}.{i}"); 
                                    }
                                    break;
            """);
                    continue;
                }

                sb.AppendLine($$"""
                                case {{type.Type.ToGlobalName()}} {{type.SafeName}}:
                                    Process_{{type.Type.ToSafeGlobalName()}}(context, {{type.SafeName}}, $"{path}.{key}"); 
                                    break;
            """);
            }

            sb.AppendLine("""
                                default:
                                    throw new global::System.InvalidOperationException("Unknown type");
                            }
                        }
                    }
            """);
        }

        return sb.ToString();
    }

    private static string GenerateTypeName(ITypeSymbol type)
    {
        return type.IsAnonymousType
            ? type.BaseType!.ToGlobalName()
            : type.ToGlobalName();
    }

    private static string GenerateAccessor(IPropertySymbol propertySymbol, INamedTypeSymbol uploadType)
    {
        return propertySymbol.Type switch
        {
            INamedTypeSymbol namedType when SymbolEqualityComparer.Default.Equals(namedType, uploadType) =>
                $@"         
            {{
                var propertyValue = {GenerateGetter(propertySymbol)};
                if (propertyValue is not null)
                {{
                    var index = context.Uploads.Count;
                    var uploadEntry = new UploadEntry
                    {{
                        Index = index,
                        Path = {GeneratePath(propertySymbol)},
                        Getter = () => (ZeroQL.Upload)propertyValue,
                    }};
                    context.Uploads.Add(uploadEntry);
                }}
            }}
",
            INamedTypeSymbol namedType when namedType.SpecialType == SpecialType.System_Object =>
                $@"         
            {{
                var propertyValue = {GenerateGetter(propertySymbol)};
                if (propertyValue is not null)
                {{
                    ProcessObject(context, propertyValue, {GeneratePath(propertySymbol)});
                }}
            }}
",
            INamedTypeSymbol namedType => @$"Process_{namedType.ToSafeGlobalName()}(context, {GenerateGetter(propertySymbol)}, {GeneratePath(propertySymbol)});",
            IArrayTypeSymbol arrayType =>
                @$"
            {{
                var propertyValue = ({GenerateTypeName(propertySymbol.Type)}){GenerateGetter(propertySymbol)};
                if (propertyValue is not null)
                {{
                    for(var i = 0; i < propertyValue.Length; i++)
                    {{
                        Process_{arrayType.ElementType.ToSafeGlobalName()}(context, propertyValue[i], path + $"".{propertySymbol.Name.FirstToLower()}.{{i}}"");
                    }}
                }};
            }}
",
            _ => $@""
        };
    }

    private static string GenerateGetter(IPropertySymbol propertySymbol)
    {
        return propertySymbol.ContainingType switch
        {
            { IsAnonymousType: true } => $@"ZeroQLReflectionCache.Get(value, ""{propertySymbol.Name}"")",
            _ => $@"value.{propertySymbol.Name}",
        };
    }

    private static string GeneratePath(IPropertySymbol propertySymbol)
    {
        return propertySymbol.Type switch
        {
            _ => $@"$""{{path}}.{propertySymbol.Name.FirstToLower()}""",
        };
    }

    public static string GenerateRequestPreparations(string inputType, GraphQLQueryExecutionStrategy executionStrategy, Dictionary<string, UploadInfoByType> infoForTypes)
    {
        if (infoForTypes.Empty())
        {
            return RequestWithoutUpload();
        }

        var processSource = executionStrategy switch
        {
            GraphQLQueryExecutionStrategy.LambdaWithClosure => $@"Process(context, variables, ""variables"");",
            _ => $@"Process_{infoForTypes[inputType].Type.ToSafeGlobalName()}(context, variables, ""variables"");"
        };

        return $@"
                var context = new MultipartFormDataContentContext();
                {processSource}

                var content = new MultipartFormDataContent();
                content.Headers.Add(""GraphQL-preflight"", ""1"");

                var queryJson = qlClient.Serialization.Serialize(queryRequest);
                content.Add(new StringContent(queryJson), ""operations"");

                var map = context.Uploads.ToDictionary(o => o.Index,  o => new [] {{ o.Path }});
                var mapJson = qlClient.Serialization.Serialize(map);
                content.Add(new StringContent(mapJson), ""map"");
                foreach(var uploadInfo in context.Uploads)
                {{
                    var upload = uploadInfo.Getter();
                    content.Add(new StreamContent(upload.Stream), uploadInfo.Index.ToString(), upload.FileName);
                }};
";
    }

    private static string RequestWithoutUpload()
    {
        return @"
                var requestJson = qlClient.Serialization.Serialize(queryRequest); 
                var content = new StringContent(requestJson, Encoding.UTF8, ""application/json"");";
    }
}