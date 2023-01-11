using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using ZeroQL.CLI;

namespace ZeroQL.Tests.CLI;

[UsesVerify]
public class JsonSchema
{
    [Fact]
    public async Task GenerateSchema()
    {
        var jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };
        var jsonSchemaGeneratorSettings = new JsonSchemaGeneratorSettings()
        {
            SerializerSettings = jsonSerializerSettings
        };
        
        var schema = NJsonSchema.JsonSchema.FromType<ZeroQLFileConfig>(jsonSchemaGeneratorSettings);

        await Verify(schema.ToJson(), "json")
            .UseFileName("schema")
            .UseDirectory("../../..");
    }
}