using System;

namespace ZeroQL.Core;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class GraphQLFieldSelector : Attribute
{
    
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class GraphQLFragment : Attribute
{
    
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class GraphQLQueryTemplate : Attribute
{
    public GraphQLQueryTemplate(string query)
    {
        Query = query;
    }

    public string Query { get; }
}