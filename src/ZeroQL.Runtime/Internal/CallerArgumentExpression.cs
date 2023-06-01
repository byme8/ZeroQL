using System;

#if NETSTANDARD
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

internal class CallerArgumentExpression : Attribute
{
    public CallerArgumentExpression(string expression)
    {
        Expression = expression;
    }

    public string Expression { get; }
}
#endif