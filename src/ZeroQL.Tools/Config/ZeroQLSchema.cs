using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Validation;
using ZeroQL.Core.Config;

namespace ZeroQL.Config;

public class ZeroQLSchema
{
    public static JsonSchema GetJsonSchema()
    {
        var jsonSchemaGeneratorSettings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = GetJsonSerializerOptions()
        };

        var schema = JsonSchema.FromType<ZeroQLFileConfig>(jsonSchemaGeneratorSettings);
        schema.ActualProperties.GetValueOrDefault("graphql")?.IsRequired = true;
        schema.ActualProperties.GetValueOrDefault("namespace")?.IsRequired = true;
        schema.ActualProperties.GetValueOrDefault("clientName")?.IsRequired = true;

        return schema;
    }

    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = ZeroQLJsonContext.Default,
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
        return options;
    }

    public static string GetHumanReadableErrorMessage(ValidationErrorKind errorKind)
    {
        var splitByCamelCase = Regex.Replace(errorKind.ToString(), "(\\B[A-Z])", " $1");
        return splitByCamelCase;
    }
}