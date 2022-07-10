using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroQL.SourceGenerators.Generator
{
    public class QueryMethodSelector : ISyntaxReceiver
    {
        public List<InvocationExpressionSyntax> Invocations { get; } = new List<InvocationExpressionSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.ToString() is "Query" or "Mutation")
            {
                Invocations.Add(invocation);
            }
        }
    }
}