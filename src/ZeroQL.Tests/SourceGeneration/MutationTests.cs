using FluentAssertions;
using Xunit;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class MutationTests : IntegrationTest
{
    [Fact]
    public async Task SimpleMutation()
    {
        var csharpQuery = "Mutation(static m => m.AddUser(\"Jon\", \"Smith\", o => o.FirstName))";
        var graphqlQuery = @"mutation { addUser(firstName: ""Jon"", lastName: ""Smith"") { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FULL_ME_QUERY, csharpQuery));

        var result = (GraphQLResult<string>)await project.Validate(graphqlQuery);
        result.Data.Should().Be("Jon");
    }

    [Fact]
    public async Task MutationWithVariables()
    {
        var csharpQuery = "Mutation(new { FirstName = \"Jon\", LastName = \"Smith\"}, static (v, m) => m.AddUser(v.FirstName, v.LastName, o => o.FirstName))";
        var graphqlQuery = @"mutation ($firstName: String!, $lastName: String!) { addUser(firstName: $firstName, lastName: $lastName) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FULL_ME_QUERY, csharpQuery));

        var result = (GraphQLResult<string>)await project.Validate(graphqlQuery);
        result.Data.Should().Be("Jon");
    }
}