using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NJsonSchema.Generation;
using NJsonSchema.Validation;
using ZeroQL.Core.Config;

namespace ZeroQL.Config;

public class ZeroQLSchema
{
    public static NJsonSchema.JsonSchema GetJsonSchema()
    {
        var jsonSchemaGeneratorSettings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = GetJsonSerializerSettings()
        };

        var schema = NJsonSchema.JsonSchema.FromType<ZeroQLFileConfig>(jsonSchemaGeneratorSettings);
        return schema;
    }

    public static JsonSerializerOptions GetJsonSerializerSettings()
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            
            Converters =
            {
                new JsonStringEnumConverter()
            },
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition =  JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
        return jsonSerializerOptions;
    }

    public static string GetHumanReadableErrorMessage(ValidationErrorKind errorKind)
    {
        var splitByCamelCase = Regex.Replace(errorKind.ToString(), "(\\B[A-Z])", " $1");
        return splitByCamelCase;
    }
}