using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using NJsonSchema.Validation;
using ZeroQL.Core.Config;

namespace ZeroQL.Config;

public class ZeroQLSchema
{
    public static NJsonSchema.JsonSchema GetJsonSchema()
    {
        var jsonSchemaGeneratorSettings = new JsonSchemaGeneratorSettings()
        {
            SerializerSettings = GetJsonSerializerSettings()
        };

        var schema = NJsonSchema.JsonSchema.FromType<ZeroQLFileConfig>(jsonSchemaGeneratorSettings);
        return schema;
    }

    public static JsonSerializerSettings GetJsonSerializerSettings()
    {
        var jsonSerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter()
            },
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        return jsonSerializerSettings;
    }

    public static string GetHumanReadableErrorMessage(ValidationErrorKind errorKind)
    {
        var splitByCamelCase = Regex.Replace(errorKind.ToString(), "(\\B[A-Z])", " $1");
        return splitByCamelCase;
    }
}