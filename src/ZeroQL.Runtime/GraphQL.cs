using System;

namespace ZeroQL;

public abstract record GraphQL<TQuery, TResult>
{
    public abstract TResult Execute(TQuery query);
}

public static class GraphQLSyntaxExtensions
{
    [GraphQLSyntax]
    public static Selector<TValue>? On<TValue>(this object value)
    {
        if (value is not TValue targetValue)
        {
            return null;
        }
        
        return new Selector<TValue>(targetValue);
    }
    
    [GraphQLSyntax]
    public static TResult? Select<TValue, TResult>(this Selector<TValue>? container, Func<TValue, TResult> selector)
    {
        if (!container.HasValue)
        {
            return default;
        }
        
        if (container.Value.Value is null)
        {
            return default;
        }
        
        return selector(container.Value.Value);
    }
}

public struct Selector<TValue>
{
    public TValue? Value { get; }

    public Selector(TValue? value)
    {
        this.Value = value;
    }
}