using System.Reflection;
using FluentAssertions;
using LinqQL.Core;
using LinqQL.Tests.Core;
using LinqQL.Tests.Data;
using Xunit;

namespace LinqQL.Tests.SourceGeneration;

public class GraphQLQuerySourceGeneratorTests : IntegrationTest
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

        var project = TestProject.Project;

        var assembly = await project.CompileToRealAssembly();

        var execute = assembly.GetType("LinqQL.TestApp.Program")
            .GetMethod("Execute", BindingFlags.Static | BindingFlags.Public)
            .CreateDelegate(typeof(Func<Task<object>>)) as Func<Task<object>>;

        var result = (GraphQLResponse<string>)(await execute());

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
        result.Data.Should().Be("Jon");
    }
}