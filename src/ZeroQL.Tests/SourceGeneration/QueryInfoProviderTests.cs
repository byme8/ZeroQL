using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class QueryInfoProviderTests
{
    [Fact]
    public async Task QueryProviderWorks()
    {
        var csharpQuery = """
                var response = QueryInfoProvider.Materialize<Query>(m => m.Me(o => o.FirstName));
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }
    
    [Fact]
    public async Task MutationProviderWorks()
    {
        var csharpQuery = """
                var firstName = "John";
                var lastName = "Doe";
                var response = QueryInfoProvider.Materialize<Mutation>(m => m.AddUser(firstName, lastName, o => o.FirstName));
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }
}