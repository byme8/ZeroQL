using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

[UsesVerify]
public class OnSyntaxTests : IntegrationTest
{
    [Fact]
    public async Task Interfaces()
    {
        var csharpQuery = """
                static q => q.Figures(
                    o => new
                    {
                        o.Perimeter,
                        Circle = o.On<Circle>()
                            .Select(oo => new { oo.Radius })
                    })
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var result = await project.Execute();

        await Verify(result);
    }
}