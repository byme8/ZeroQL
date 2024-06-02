using Microsoft.CodeAnalysis;
using ZeroQL.SourceGenerators.Analyzers;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class OnSyntaxTests : IntegrationTest
{
    [Fact]
    public async Task Interfaces()
    {
        var csharpQuery = """
                static q => q.Figures(
                    o => new
                    {
                        o.Id,
                        o.Perimeter,
                        CreatorId = o.Creator(oo => oo.Id),
                        Circle = o.On<Circle>()
                            .Select(oo => new { oo.Radius, Center = oo.Center(ooo => new { ooo.Id, ooo.X, ooo.Y }) }),
                        Square = o.On<Square>()
                            .Select(oo => new 
                                { 
                                    TopLeft = oo.TopLeft(ooo => new { ooo.Id, ooo.X, ooo.Y }), 
                                    BottomRight = oo.BottomRight(ooo => new { ooo.Id, ooo.X, ooo.Y }) 
                                })
                    })
                """;

        var result = await TestProject.Project.Execute(csharpQuery);

        await Verify(result);
    }
    
    [Fact]
    public async Task InterfacesDominatedPropertyAndInterfaceProperty()
    {
        var csharpQuery = """
                          static q => q.Figures(
                              o => new
                              {
                                  CreatorId = o.Creator(oo => oo.Id),
                                  SquareCreatorId = o.On<Square>()
                                      .Select(oo => oo.Creator("name", ooo => ooo.Id))
                              })
                          """;

        var result = await TestProject.Project.Execute(csharpQuery);

        await Verify(result);
    }
    
    [Fact]
    public async Task InterfacesInterfaceProperty()
    {
        var csharpQuery = """
                          static q => q.Figures(
                              o => new
                              {
                                  CreatorId = o.Creator(oo => oo.Id),
                                  SquareCreatorId = o.On<Square>()
                                      .Select(oo => oo.Creator("name", ooo => ooo.Id))
                              })
                          """;

        var result = await TestProject.Project.Execute(csharpQuery);

        await Verify(result);
    }
    
    [Fact]
    public async Task InterfacesDominatedProperty()
    {
        var csharpQuery = """
                          static q => q.Figures(
                              o => new
                              {
                                  SquareCreatorId = o.On<Square>()
                                      .Select(oo => oo.Creator("name", ooo => ooo.Id))
                              })
                          """;

        var result = await TestProject.Project.Execute(csharpQuery);

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
                        .Select(oo => new { oo.ImageUrl, oo.Resolution }),
                    Text = o.On<TextContent>()
                        .Select(oo => new { oo.Text }),
                    Figure = o.On<FigureContent>()
                        .Select(oo => oo.Figure(ooo => new { Circle = ooo.On<Circle>().Select(oooo => new { oooo.Radius }) }))
                })
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var result = await project.Execute();

        await Verify(result);
    }

    [Fact]
    public async Task AppliedToWrongType()
    {
        var csharpQuery = """
                static q => q.Figures(
                o => new
                {
                    Circle = o.On<ImageContent>()
                        .Select(oo => new { oo.ImageUrl, oo.Resolution }),
                })
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();

        await Verify(diagnostics
            .Where(o => o.Severity == DiagnosticSeverity.Error)
            .Select(o => new
            {
                o.Descriptor.Id,
                Highlighted = o.Location.SourceTree!.GetRoot().FindNode(o.Location.SourceSpan).ToString(),
                Message = o.GetMessage()
            }));
    }
}