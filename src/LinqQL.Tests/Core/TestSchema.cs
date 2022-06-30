namespace LinqQL.Tests.Core
{
    public static class TestSchema
    {
        public static string RawSchema { get; }

        static TestSchema()
        {
            RawSchema = File.ReadAllText("../../../../LinqQL.TestApp/schema.graphql");
        }
    }
}