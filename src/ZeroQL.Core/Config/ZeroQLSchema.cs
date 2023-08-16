using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using NJsonSchema.Validation;

namespace ZeroQL.Core.Config;

public class ZeroQLSchema
{
    public static NJsonSchema.JsonSchema GetJsonSchema()
    {
        var jsonSerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>()
            {
                new StringEnumConverter()
            },
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            }
        };
        var jsonSchemaGeneratorSettings = new JsonSchemaGeneratorSettings()
        {
            SerializerSettings = jsonSerializerSettings
        };

        var schema = NJsonSchema.JsonSchema.FromType<ZeroQLFileConfig>(jsonSchemaGeneratorSettings);
        return schema;
    }

    public static string GetHumanReadableErrorMessage(ValidationErrorKind errorKind) 
    {
        var splitByCamelCase = Regex.Replace(errorKind.ToString(), "(\\B[A-Z])", " $1");
        return splitByCamelCase;
    }
}