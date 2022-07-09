using FluentAssertions;
using LinqQL.Core;
using LinqQL.Tests.Core;
using LinqQL.Tests.Data;
using Xunit;

namespace LinqQL.Tests.SourceGeneration;

public class VariablesTests : IntegrationTest
{
    [Fact]
    public async Task SupportsPassedArgumentInQuery()
    {
        var csharpQuery = "static (i, q) => q.User(i.Id, o => o.Id)";
        var graphqlQuery = @"query ($id: Int!) { user(id: $id) { id } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, "new { Id = -431 }, " + csharpQuery));

        var response = (GraphQLResult<int>)await project.Validate(graphqlQuery);

        response.Data.Should().Be(-431);
    }

    [Fact]
    public async Task SupportsAnonymousTypeWithArgumentQuery()
    {
        var csharpQuery = "static (i, q) => new { User = q.User(i.Id, o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"query ($id: Int!) { user(id: $id) { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, "new { Id = 42 }, " + csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportsPassedArgumentInNamedQuery()
    {
        var csharpQuery = "static (i, q) => q.User(i.Id, o => o.Id)";
        var graphqlQuery = @"query TestQuery($id: Int!) { user(id: $id) { id } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, "\"TestQuery\", new { Id = -431 }, " + csharpQuery));

        var result = (GraphQLResult<int>)await project.Validate(graphqlQuery);

        result.Data.Should().Be(-431);
    }

    [Fact]
    public async Task SupportForEnumsAsArgument()
    {
        var csharpQuery = "static q => q.UsersByKind(UserKind.BAD, 0, 10, o => o.FirstName)";
        var graphqlQuery = @"query { usersByKind(kind: BAD, page: 0, size: 10) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }
    
    [Fact]
    public async Task VariablesCanBeSeparate()
    {
        var csharpQuery = "variables, static (i, q) => q.User(i.Id, o => o.FirstName)";
        var graphqlQuery = @"query ($id: Int!) { user(id: $id) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                ("// place to replace", "var variables = new { Id = 1 };"),
                (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }
    
    // var variables = new { Id = 1 };
    // var response = await qlClient.Query(variables, static (i, q) => q.User(i.Id, o => o.FirstName));

}