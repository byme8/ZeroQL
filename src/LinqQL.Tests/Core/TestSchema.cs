namespace LinqQL.Tests.Core;

public static class TestSchema
{

    static TestSchema()
    {
        RawSchema = File.ReadAllText("../../../../LinqQL.TestApp/schema.graphql");
    }

    public static string RawSchema { get; }
}