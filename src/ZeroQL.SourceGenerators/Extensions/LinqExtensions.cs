using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq;

public static class LinqExtensions

{
    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) 
        => dictionary.TryGetValue(key, out var value) ? value : default;
}