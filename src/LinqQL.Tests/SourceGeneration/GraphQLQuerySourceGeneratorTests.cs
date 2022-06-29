using System.Reflection;
using FluentAssertions;
using LinqQL.Core;
using LinqQL.Tests.Core;
using LinqQL.Tests.Data;
using Xunit;

namespace LinqQL.Tests.SourceGeneration;

public class GraphQLQuerySourceGeneratorTests
{
    [Fact]
    public async Task CompilationWorks()
    {
        var project = await TestProject.Project.CompileToRealAssembly();
    }

    [Fact]
    public async Task SimpleQuery()
    {
        var csharpQuery = @"static q => q.Me(o => o.FirstName)";
        var graphqlQuery = @"query { me { firstName } }";

        var queryCall = $"qlClient.Query({csharpQuery});";
        var project = await TestProject.Project.ReplacePartOfDocumentAsync("Program.cs", "// place for query", queryCall);

        var assembly = await project.CompileToRealAssembly();

        var result = assembly.GetType("LinqQL.TestApp.Program")
            .GetMethod("Stub", BindingFlags.Static | BindingFlags.Public)
            .Invoke(null, null);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }
}