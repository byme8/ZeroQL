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
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullMeQuery, csharpQuery));

        var result = (GraphQLResult<string>)await project.Validate(graphqlQuery);
        result.Data.Should().Be("Jon");
    }

    [Fact]
    public async Task MutationWithVariables()
    {
        var csharpQuery = """
                              var firstName = "Jon";
                              var lastName = "Smith";
                              var response = await qlClient.Mutation(m => m.AddUser(firstName, lastName, o => o.FirstName));
                          """;

        var response = await TestProject.Project.ExecuteFullLine(csharpQuery);
        
        await Verify(response);
    }

    [Fact]
    public async Task MutationWithCustomEnums()
    {
        var csharpQuery = "Mutation(static m => m.AddUserKindPascal(UserKindPascal.Default))";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullMeQuery, csharpQuery));

        var result = await project.Execute();

        await Verify(result);
    }

    [Fact]
    public async Task MutationWithCustomEnumsAsVariable()
    {
        var csharpQuery = """
                              var input = UserKindPascal.SupperGood;
                              var response = await qlClient.Mutation(m => m.AddUserKindPascal(input));
                          """;

        var response = await TestProject.Project.ExecuteFullLine(csharpQuery);
        
        await Verify(response);
    }

    [Fact]
    public async Task ReplacementTypeWorks()
    {
        var csharpQuery = """
                var limit = new LimitInputZeroQL { Limit = 10 };
                var response = await qlClient.Mutation(m => m.AddLimit(limit, o => o.Limit));
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }
    
    [Fact]
    public async Task OverrideScalarsAccounted()
    {
        var csharpQuery = """
                var guid = Guid.NewGuid();
                var response = await qlClient.Mutation(m => m.CreateUserId(guid));
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }
}