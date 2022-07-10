namespace ZeroQL.Tests.Core;

public static class TestSchema
{

    static TestSchema()
    {
        RawSchema = File.ReadAllText("../../../../ZeroQL.TestApp/schema.graphql");
    }

    public static string RawSchema { get; }
}