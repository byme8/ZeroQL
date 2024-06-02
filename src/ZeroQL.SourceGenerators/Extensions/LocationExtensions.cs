using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.SourceGenerators.Extensions;

public static class LocationExtensions
{
    public static Location GetLocationForPreview(this ExpressionSyntax node) => node switch
    {
        InvocationExpressionSyntax invocation => invocation.Expression.GetLocationForPreview(),
        MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
        _ => node.GetLocation()
    };
}