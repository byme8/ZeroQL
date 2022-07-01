using System.Reflection;
using FluentAssertions;
using LinqQL.Core;
using LinqQL.Tests.Core;
using LinqQL.Tests.Data;
using Xunit;

namespace LinqQL.Tests.SourceGeneration;

public class GraphQLQuerySourceGeneratorTests : IntegrationTest
{
    private const string MeQuery = @"static q => q.Me(o => o.FirstName)";

    [Fact]
    public async Task CompilationWorks()
    {
        await TestProject.Project.CompileToRealAssembly();
    }

    [Fact]
    public async Task SimpleQuery()
    {
        var graphqlQuery = @"query { me { firstName } }";

        var project = TestProject.Project;

        var assembly = await project.CompileToRealAssembly();

        var execute = CreateExecuteDelegate(assembly);

        var result = (GraphQLResponse<string>)await execute();

        result.Data.Should().Be("Jon");
        GraphQLQueryStore.Query[MeQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task AnonymousTypeQuery()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName })";
        var graphqlQuery = @"query { me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        CreateExecuteDelegate(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task AnonymousTypeInQueryRoot()
    {
        var csharpQuery = "static q => new { Me = q.Me(o => new { o.FirstName }) }";
        var graphqlQuery = @"query { me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        CreateExecuteDelegate(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    private static Func<Task<object>> CreateExecuteDelegate(Assembly assembly)
    {
        return (assembly.GetType("LinqQL.TestApp.Program")!
            .GetMethod("Execute", BindingFlags.Static | BindingFlags.Public)!
            .CreateDelegate(typeof(Func<Task<object>>)) as Func<Task<object>>)!;
    }

    [Fact]
    public async Task AnonymousTypeWithMultipleFieldsQuery()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName, o.LastName })";
        var graphqlQuery = @"query { me { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        CreateExecuteDelegate(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact(Skip = "Think how to fix after release")]
    public async Task AnonymousTypeWithMultipleIdenticalFieldsInRootQuery()
    {
        var csharpQuery = "static q => new { Me1 = q.Me(o => new { o.FirstName, o.LastName }), Me2 = q.Me(o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"query { m1: me { firstName lastName } m2: me { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        CreateExecuteDelegate(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task AnonymousTypeWithConstantArgumentQuery()
    {
        var csharpQuery = "static q => new { User = q.User(42, o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"query { user(id: 42) { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        CreateExecuteDelegate(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task AnonymousTypeWithArgumentQuery()
    {
        var csharpQuery = "static (i, q) => new { User = q.User(i.Id, o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"query ($id: Int!) { user(id: $id) { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, "new { Id = 42 }, " + csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        CreateExecuteDelegate(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task AnonymousTypeWithPassedArgumentQuery()
    {
        var csharpQuery = "static (i, q) => q.User(i.Id, o => o.Id)";
        var graphqlQuery = @"query ($id: Int!) { user(id: $id) { id } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, "new { Id = -431 }, " + csharpQuery));

        var assembly = await project.CompileToRealAssembly();
        var execute = CreateExecuteDelegate(assembly);
        var result = (GraphQLResponse<int>)await execute();

        result.Data.Should().Be(-431);
        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportForEnums()
    {
        var csharpQuery = "static q => q.Me(o => o.UserKind)";
        var graphqlQuery = @"query { me { userKind } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();
        CreateExecuteDelegate(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportForArray()
    {
        // new { Filter = new UserFilterInput { UserKind = UserKind.GOOD} }, static (i, q) => q.Users(i.Filter, 0,  10, o => o.FirstName));

        var arguments = "new { Filter = new UserFilterInput { UserKind = UserKind.GOOD} }";
        var csharpQuery = "static (i, q) => q.Users(i.Filter, 0,  10, o => o.FirstName)";
        var graphqlQuery = @"query ($filter: UserFilterInput!) { users(filter: $filter, page: 0, size: 10) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, $"{arguments}, {csharpQuery}"));

        var assembly = await project.CompileToRealAssembly();
        CreateExecuteDelegate(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }
}