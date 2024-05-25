using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Linq;

public static class LinqExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) 
        => dictionary.TryGetValue(key, out var value) ? value : default;
    
    public static bool Empty<T>(this IReadOnlyList<T> enumerable) => enumerable.Count == 0;
    
    public static bool Empty<T>(this IEnumerable<T> enumerable) => !enumerable.Any();
}