using System.Collections.Immutable;
using System.Reflection;
using ZeroQL.SourceGenerators.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ZeroQL.Tests.Core;

public static class TestExtensions
{
    public static async Task<Project> ReplacePartOfDocumentAsync(this Project project,
        string documentName,
        string textToReplace,
        string newText)
    {
        var document = project.Documents.First(o => o.Name == documentName);
        var text = await document.GetTextAsync();
        return document
            .WithText(SourceText.From(text.ToString().Replace(textToReplace, newText)))
            .Project;
    }

    public static async Task<Project> ReplacePartOfDocumentAsync(this Project project,
        string documentName,
        params (string TextToReplace, string NewText)[] places)
    {
        foreach (var place in places)
        {
            project = await project.ReplacePartOfDocumentAsync(documentName, place.TextToReplace, place.NewText);
        }

        return project;
    }

    public static async Task<Project> ReplacePartOfDocumentAsync(this Project project,
        params (string ProjectName, string DocumentName, string TextToReplace, string NewText)[] places)
    {
        var solution = project.Solution;
        foreach (var place in places)
        {
            var newProject = await solution.Projects
                .First(o => o.Name == place.ProjectName)
                .ReplacePartOfDocumentAsync(place.DocumentName, (place.TextToReplace, place.NewText));

            solution = newProject.Solution;
        }

        return solution.Projects.First(o => o.Name == project.Name);
    }

    public static async Task<Assembly> CompileToRealAssembly(this Project project)
    {
        var compilation = await project.GetCompilationAsync();
        var analyzerResults = await compilation!
            .WithAnalyzers(ImmutableArray.Create(new DiagnosticAnalyzer[]
            {
                new QueryLambdaAnalyzer()
            }))
            .GetAllDiagnosticsAsync();

        var error = compilation!.GetDiagnostics()
            .Concat(analyzerResults)
            .FirstOrDefault(o => o.Severity == DiagnosticSeverity.Error);

        if (error != null)
        {
            throw new Exception(error.GetMessage());
        }

        using (var memoryStream = new MemoryStream())
        {
            compilation.Emit(memoryStream);
            var bytes = memoryStream.ToArray();
            var assembly = Assembly.Load(bytes);

            return assembly;
        }
    }

    public static async Task<Diagnostic[]?> ApplyGenerator(this Project project, ISourceGenerator generator)
    {
        var compilation = await project.GetCompilationAsync();

        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGenerators(compilation!);

        var result = driver.GetRunResult();
        return result.Diagnostics.ToArray();
    }

    public static async Task<Diagnostic[]?> ApplyAnalyzer(this Project project, DiagnosticAnalyzer analyzer)
    {
        var compilation = await project.GetCompilationAsync();
        var analyzerResults = await compilation!
            .WithAnalyzers(ImmutableArray.Create(new[]
            {
                analyzer
            }))
            .GetAllDiagnosticsAsync();

        var error = compilation!.GetDiagnostics()
            .Concat(analyzerResults);

        return error.ToArray();
    }
}