// ReSharper disable once CheckNamespace

using System;

// ReSharper disable once CheckNamespace
namespace ZeroQL;

[Obsolete("Request syntax is deprecated because it is not compatible with AOT runtimes. It will be removed in the future releases.")]
public abstract record GraphQL<TQuery, TResult>
{
    public abstract TResult Execute(TQuery query);
}