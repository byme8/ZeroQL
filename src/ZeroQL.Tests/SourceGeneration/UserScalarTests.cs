using System.Text.Json;
using Namotion.Reflection;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class UserScalarTests: IntegrationTest
{
    [Fact]
    public async Task InstantScalarTypeWorks()
    {
        var csharpQuery = """
            var instantResponse = await qlClient.Query(static q => q.Instant);
            var input = new { Instant = instantResponse.Data };
            var response = await qlClient.Mutation(input, static (i, m) => m.CreateInstant(i.Instant));
            """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response)
            .DontScrubDateTimes();
    }
    
    [Fact]
    public async Task JsonElementTypeWorks()
    {
        var csharpQuery = "static q => q.JsonUsersElement";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var response = await project.Execute();
        var value = response.TryGetPropertyValue<JsonElement>("Data");

        await Verify(value.ToString());
    }
    
    [Fact]
    public async Task JsonDocumentTypeWorks()
    {
        var csharpQuery = "static q => q.JsonUsersDocument(o => o.RootElement)";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var response = await project.Execute();
        var value = response.TryGetPropertyValue<JsonElement>("Data");

        await Verify(value.ToString());
    }
}