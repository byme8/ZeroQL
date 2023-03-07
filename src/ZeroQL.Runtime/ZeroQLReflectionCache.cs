using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ZeroQL;

public class ZeroQLReflectionCache
{
    private static readonly Dictionary<(Type, string), Func<object, object>> Cache = new();

    public static object Get(object target, string propertyName)
    {
        var type = target.GetType();
        if (Cache.TryGetValue((type, propertyName), out var func))
        {
            return func.Invoke(target);
        }

        var propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;
        var getter = CreateGetter(propertyInfo);

        Cache.Add((type, propertyName), getter);

        return getter.Invoke(target);
    }
    
    public static Func<object, object> CreateGetter(PropertyInfo property)
    {
        var objParam = Expression.Parameter(typeof(object), "o");
        var lambda = Expression.Lambda(typeof(Func<object, object>), Expression.Property(Expression.Convert(objParam, property.DeclaringType!), property.Name), objParam);
        return (Func<object, object>)lambda.Compile();
    }
}