using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

[UsesVerify]
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
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FULL_LINE, csharpQuery));

        var response = await project.Execute();

        await Verify(response)
            .DontScrubDateTimes();
    }
}