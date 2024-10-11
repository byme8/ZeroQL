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
public class GraphQLQueryTemplate(string query) : Attribute
{
    public string Query { get; } = query;
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.GenericParameter)]
public class GraphQLLambda : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.GenericParameter)]
public class StaticLambda : Attribute
{
}

[AttributeUsage(AttributeTargets.All)]
public class ErrorAttribute(string message) : Attribute
{
    public string Message { get; } = message;
}