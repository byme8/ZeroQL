using System;

namespace ZeroQL;

[AttributeUsage(
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Field,
    Inherited = false)]
public class GraphQLNameAttribute : Attribute
{
    public GraphQLNameAttribute(string name)
    {
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, Inherited = false)]
public class GraphQLTypeAttribute : Attribute
{
    public GraphQLTypeAttribute(string name)
    {
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class GraphQLSyntax : Attribute
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