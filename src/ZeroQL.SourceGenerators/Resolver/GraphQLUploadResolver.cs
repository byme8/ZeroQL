using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ZeroQL.SourceGenerators.Resolver;

public class GraphQLUploadResolver
{
    public static string GenerateUploadsSelectors(UploadInfoByType[] types, INamedTypeSymbol uploadType)
    {
        if (!types.Any())
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var type in types.Where(o => o.Type is not IArrayTypeSymbol))
        {
            var name = GenerateTypeName(type.Type);

            sb.Append($@"
        private static void Process_{type.Type.ToSafeGlobalName()}(MultipartFormDataContentContext context, {name} value, string path)
        {{
{type.UploadProperties.Select(o => GenerateAccessor(type, o, uploadType)).JoinWithNewLine()}
        }}");

        }

        return sb.ToString();
    }

    private static string GenerateTypeName(ITypeSymbol type)
    {
        return type.IsAnonymousType
            ? type.BaseType!.ToGlobalName()
            : type.ToGlobalName();
    }

    private static string GenerateAccessor(UploadInfoByType type, IPropertySymbol propertySymbol, INamedTypeSymbol uploadType)
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

    public static string GenerateRequestPreparations(string graphQLInputTypeName, Dictionary<string, UploadInfoByType> infoForTypes)
    {
        if (!infoForTypes.TryGetValue(graphQLInputTypeName, out var root))
        {
            return RequestWithoutUpload();
        }

        return $@"
                var context = new MultipartFormDataContentContext();
                Process_{root.Type.ToSafeGlobalName()}(context, variables, ""variables"");

                var content = new MultipartFormDataContent();

                var queryJson = JsonSerializer.Serialize(queryRequest, ZeroQLJsonOptions.Options);
                content.Add(new StringContent(queryJson), ""operations"");

                var map = context.Uploads.ToDictionary(o => o.Index,  o => new [] {{ o.Path }});
                var mapJson = JsonSerializer.Serialize(map, ZeroQLJsonOptions.Options);
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
                var requestJson = JsonSerializer.Serialize(queryRequest, ZeroQLJsonOptions.Options); 
                var content = new StringContent(requestJson, Encoding.UTF8, ""application/json"");";
    }
}