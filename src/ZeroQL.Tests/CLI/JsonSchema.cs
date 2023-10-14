using ZeroQL.CLI;
using ZeroQL.Config;
using ZeroQL.Core.Config;

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