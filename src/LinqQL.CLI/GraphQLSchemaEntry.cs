namespace LinqQL.CLI
{
    public class GraphQLSchemaEntry
    {
        public GraphQLSchemaEntry(string schema, string ns, string queryName)
        {
            Schema = schema;
            Namespace = ns;
            QueryName = queryName;
        }

        public string Schema { get; }

        public string Namespace { get; }

        public string QueryName { get; }
    }
}