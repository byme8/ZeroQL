using System.Reflection;
using FluentAssertions;
using LinqQL.Core;
using LinqQL.SourceGenerators;
using LinqQL.SourceGenerators.Analyzers;
using LinqQL.SourceGenerators.Generator;
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
        var graphqlQuery = @"{ me { firstName } }";

        var project = TestProject.Project;

        var assembly = await project.CompileToRealAssembly();

        var result = (GraphQLResult<string>)await ExecuteRequest(assembly);

        result.Data.Should().Be("Jon");
        GraphQLQueryStore.Query[MeQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportForAnonymousType()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName })";
        var graphqlQuery = @"{ me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportForMultipleMembers()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName.Length })";
        var graphqlQuery = @"{ me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task FailsWhenAccessingVariablesOutLambdaScope()
    {
        var csharpQuery = "static q => q.Me(o => q.Me(o => o.FirstName))";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var diagnostics = await project.ApplyGenerator(new GraphQLQuerySourceGenerator());

        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.DontUserOutScopeValues.Id);
    }

    [Fact]
    public async Task FailsOnOpenLambda()
    {
        var csharpQuery = "static q => q.Me(o => o)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var diagnostics = await project.ApplyAnalyzer(new QueryLambdaAnalyzer());
        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.OpenLambdaIsNotAllowed.Id);
    }

    [Fact]
    public async Task FailsWhenLambdaIsNotStatic()
    {
        var csharpQuery = "q => q.Me(o => o.FirstName)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var diagnostics = await project.ApplyAnalyzer(new QueryLambdaAnalyzer());

        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.OnlyStaticLambda.Id);
    }

    [Fact]
    public async Task SupportsAnonymousTypeInQueryRoot()
    {
        var csharpQuery = "static q => new { Me = q.Me(o => new { o.FirstName }) }";
        var graphqlQuery = @"{ me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    private static async Task<object> ExecuteRequest(Assembly assembly)
    {
        var method = (assembly.GetType("LinqQL.TestApp.Program")!
            .GetMethod("Execute", BindingFlags.Static | BindingFlags.Public)!
            .CreateDelegate(typeof(Func<Task<object>>)) as Func<Task<object>>)!;

        return await method.Invoke();
    }

    [Fact]
    public async Task SupportsAnonymousTypeWithMultipleFields()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName, o.LastName })";
        var graphqlQuery = @"{ me { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact(Skip = "Think how to fix after release")]
    public async Task SupportsAnonymousTypeWithMultipleIdenticalFieldsInRootQuery()
    {
        var csharpQuery = "static q => new { Me1 = q.Me(o => new { o.FirstName, o.LastName }), Me2 = q.Me(o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"{ m1: me { firstName lastName } m2: me { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportsAnonymousTypeWithConstantArgumentQuery()
    {
        var csharpQuery = "static q => new { User = q.User(42, o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"{ user(id: 42) { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportsAnonymousTypeWithArgumentQuery()
    {
        var csharpQuery = "static (i, q) => new { User = q.User(i.Id, o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"($id: Int!) { user(id: $id) { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, "new { Id = 42 }, " + csharpQuery));

        var assembly = await project.CompileToRealAssembly();

        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportsPassedArgumentInQuery()
    {
        var csharpQuery = "static (i, q) => q.User(i.Id, o => o.Id)";
        var graphqlQuery = @"($id: Int!) { user(id: $id) { id } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, "new { Id = -431 }, " + csharpQuery));

        var assembly = await project.CompileToRealAssembly();
        var result = (GraphQLResult<int>)await ExecuteRequest(assembly);

        result.Data.Should().Be(-431);
        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }
    
    [Fact]
    public async Task SupportsPassedArgumentInNamedQuery()
    {
        var csharpQuery = "static (i, q) => q.User(i.Id, o => o.Id)";
        var graphqlQuery = @"query TestQuery($id: Int!) { user(id: $id) { id } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, "\"TestQuery\", new { Id = -431 }, " + csharpQuery));

        var assembly = await project.CompileToRealAssembly();
        var result = (GraphQLResult<int>)await ExecuteRequest(assembly);

        result.Query.Should().Be(graphqlQuery);
        result.Data.Should().Be(-431);
    }

    [Fact]
    public async Task SupportForEnums()
    {
        var csharpQuery = "static q => q.Me(o => o.UserKind)";
        var graphqlQuery = @"{ me { userKind } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();
        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportForEnumsAsArgument()
    {
        var csharpQuery = "static q => q.UsersByKind(UserKind.BAD, 0, 10, o => o.FirstName)";
        var graphqlQuery = @"{ usersByKind(kind: BAD, page: 0, size: 10) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();
        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportForArray()
    {
        var arguments = "new { Filter = new UserFilterInput { UserKind = UserKind.GOOD} }";
        var csharpQuery = "static (i, q) => q.Users(i.Filter, 0,  10, o => o.FirstName)";
        var graphqlQuery = @"($filter: UserFilterInput!) { users(filter: $filter, page: 0, size: 10) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, $"{arguments}, {csharpQuery}"));

        var assembly = await project.CompileToRealAssembly();
        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }

    [Fact]
    public async Task SupportForArrayWithScalarElements()
    {
        var csharpQuery = "static q => q.UsersIds(UserKind.GOOD, 0, 10)";
        var graphqlQuery = @"{ usersIds(kind: GOOD, page: 0, size: 10)}";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, csharpQuery));

        var assembly = await project.CompileToRealAssembly();
        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }
    
    [Fact]
    public async Task SupportsQueryName()
    {
        var csharpQuery = "static q => q.Me(o => o.FirstName)";
        var graphqlQuery = @"{ me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (MeQuery, @"""Me"", " + csharpQuery));

        var assembly = await project.CompileToRealAssembly();
        await ExecuteRequest(assembly);

        GraphQLQueryStore.Query[csharpQuery].Should().Be(graphqlQuery);
    }
}