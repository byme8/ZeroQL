namespace LinqQL.Tests.Core
{
    public static class TestSchema
    {
        public static string RawSchema { get; }

        static TestSchema()
        {
            RawSchema = File.ReadAllText("Data/TestServer.graphql");
        }
    }
}