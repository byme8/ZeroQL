using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ZeroQL;

public class ZeroQLReflectionCache
{
    private static readonly Dictionary<(Type, string), Func<object, object>> _cache = new();

    public static object Get(object target, string propertyName)
    {
        var type = target.GetType();
        if (_cache.TryGetValue((type, propertyName), out var func))
        {
            return func.Invoke(target);
        }

        var propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        var getter = CreateGetter(propertyInfo);

        _cache.Add((type, propertyName), getter);

        return getter.Invoke(target);
    }
    
    public static Func<object, object> CreateGetter(PropertyInfo property)
    {
        var objParm = Expression.Parameter(typeof(object), "o");
        var lambda = Expression.Lambda(typeof(Func<object, object>), Expression.Property(Expression.Convert(objParm, property.DeclaringType), property.Name), objParm);
        return (Func<object, object>)lambda.Compile();
    }
}