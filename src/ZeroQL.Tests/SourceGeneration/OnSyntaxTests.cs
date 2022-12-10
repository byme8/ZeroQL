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
                            .Select(oo => new { oo.Radius, Center = oo.Center(ooo => new { ooo.X, ooo.Y }) }),
                        Square = o.On<Square>()
                            .Select(oo => new 
                                { 
                                    TopLeft = oo.TopLeft(ooo => new { ooo.X, ooo.Y }), 
                                    BottomRight = oo.TopLeft(ooo => new { ooo.X, ooo.Y }) 
                                })
                    })
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var result = await project.Execute();

        await Verify(result);
    }
    
    [Fact]
    public async Task Union()
    {
        var csharpQuery = """
                static q => q.Posts(
                o => new
                {
                    Image = o.On<ImageContent>()
                        .Select(oo => new { oo.ImageUrl, oo.Height }),
                    Text = o.On<TextContent>()
                        .Select(oo => new { oo.Text }),
                })
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var result = await project.Execute();

        await Verify(result);
    }
}