namespace ZeroQL.Tests.Core;

public static class TestSchema
{

    static TestSchema()
    {
        RawSchema = File.ReadAllText("../../../../TestApp/ZeroQL.TestApp.Client/schema.graphql");
    }

    public static string RawSchema { get; }
}