using ZeroQL.CLI;

namespace ZeroQL.Tests.CLI;

[UsesVerify]
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