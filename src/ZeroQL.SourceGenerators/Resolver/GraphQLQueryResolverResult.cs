using System.Collections.Generic;

namespace ZeroQL.SourceGenerators.Resolver;

public class GraphQLQueryResolverResult
{
    public string Query { get; set; }

    public Dictionary<string, GraphQLQueryVariable> Variables { get; set; }
}