using ZeroQL.Config;

namespace ZeroQL.Tests.CLI;

public class JsonSchema
{
    [Fact]
    public async Task GenerateSchema()
    {
        var schema = ZeroQLSchema.GetJsonSchema();

        await Verify(schema.ToJson(), "json")
            .UseFileName("schema")
            .UseDirectory("../../..");
    }
}