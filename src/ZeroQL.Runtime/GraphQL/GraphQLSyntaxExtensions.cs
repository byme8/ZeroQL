using System;

// ReSharper disable once CheckNamespace
namespace ZeroQL;

public interface IUnionType
{
}

public struct Selector<TValue>
    where TValue : IUnionType
{
    public TValue? Value { get; }

    public Selector(TValue? value)
    {
        this.Value = value;
    }
}

public static class GraphQLSyntaxExtensions
{
    [GraphQLSyntax]
    public static Selector<TValue>? On<TValue>(this IUnionType value)
        where TValue : IUnionType
    {
        if (value is not TValue targetValue)
        {
            return null;
        }

        return new Selector<TValue>(targetValue);
    }

    [GraphQLSyntax]
    public static TResult? Select<TValue, TResult>(this Selector<TValue>? container, Func<TValue, TResult> selector)
        where TValue : IUnionType
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