using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class OptionalParametersDetectionTests : IntegrationTest
{

    [Fact]
    public async Task NoIssues()
    {
        var csharpQuery = """
                var page = 0;
                var filter = "test"; 
                var response = await qlClient.Query(q => q.Users(page: 10, size:10, selector: o => o.FirstName));
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();

        diagnostics
            .Where(o => o.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task RequireParameterMissed()
    {
        var csharpQuery = """
                var filter = "test"; 
                var response = await qlClient.Query(q => q.UsersIds(filter: filter));
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

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
    
    [Fact]
    public async Task SelectorParameterMissed()
    {
        var csharpQuery = """
                var response = await qlClient.Query(q => q.Me());
                """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

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