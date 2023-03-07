using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using ZeroQL.SourceGenerators.Resolver;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.Tools;

public class QueryGeneratorTests : IAsyncLifetime
{
    private SemanticModel semanticModel;
    private ExpressionSyntax expression;

    public async Task InitializeAsync()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserWithRoleNameBody())";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));
        var compilation = await project.GetCompilationAsync();
        var document = project.Documents.First(o => o.Name == "Program.cs");
        var tree = await document.GetSyntaxTreeAsync();
        var node = await tree!.GetRootAsync();
        semanticModel = compilation!.GetSemanticModel(tree);

        expression = node
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(o => o.Identifier.ValueText == "Execute")
            .DescendantNodes()
            .OfType<SimpleLambdaExpressionSyntax>()
            .First();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact(Skip = "For profiling")]
    public void BenchmarkGeneration()
    {
        for (int i = 0; i < 100000; i++)
        {
            var query = GraphQLQueryResolver.Resolve(semanticModel, expression, CancellationToken.None);
        }
    }
}