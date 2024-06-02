using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using ZeroQL.SourceGenerators.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ZeroQL.SourceGenerators.Generator;

namespace ZeroQL.Tests.Core;

public static class TestExtensions
{
    public static ImmutableArray<DiagnosticAnalyzer> Analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(

        new StaticLambdaAnalyzer(),
        new QueryLambdaAnalyzer(),
        new OptionalParametersAnalyzer(),
        new QueryRequestAnalyzer(),
        new QueryOnSyntaxAnalyzer()
    );

    public static IIncrementalGenerator[] Generators =>
    [
        new GraphQLLambdaIncrementalSourceGenerator(),
        new GraphQLRequestIncrementalSourceGenerator(),
        new GraphQLFragmentTemplateIncrementalSourceGenerator(),
    ];

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
        var bytes = await project.CompileToRealAssemblyAsBytes();
        var assembly = Assembly.Load(bytes);

        return assembly;
    }

    public static async Task<byte[]> CompileToRealAssemblyAsBytes(this Project project)
    {
        var newName = $"{project.Name}.Guid{Guid.NewGuid():N}";
        var fixedProject = project.WithAssemblyName(newName);
        var compilation = await fixedProject.GetCompilationAsync();

        var generationResult = await project.ApplyGenerator(Generators);
        if (generationResult.Diagnostics.Any())
        {
            throw new Exception(generationResult.Diagnostics.First().GetMessage());
        }
        
        var analyzerResults = await compilation!
            .WithAnalyzers(Analyzers)
            .GetAllDiagnosticsAsync();

        var error = compilation!.GetDiagnostics()
            .Concat(analyzerResults)
            .FirstOrDefault(o => o.Severity == DiagnosticSeverity.Error);

        if (error != null)
        {
            throw new Exception(error.GetMessage());
        }

        using var memoryStream = new MemoryStream();
        compilation.Emit(memoryStream);
        var bytes = memoryStream.ToArray();
        return bytes;
    }

    public static async Task<GeneratorDriverRunResult> ApplyGenerator(this Project project, ISourceGenerator generator)
    {
        var compilation = await project.GetCompilationAsync();

        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGenerators(compilation!);

        var result = driver.GetRunResult();
        return result;
    }

    public static async Task<GeneratorDriverRunResult> ApplyGenerator(this Project project, params IIncrementalGenerator[] generator)
    {
        project = await project.RemoveSyntaxTreesFromReferences();

        var projectCompilation = await project.GetCompilationAsync();
        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGenerators(projectCompilation!);

        var result = driver.GetRunResult();
        return result;
    }

    public static async Task<Project> RemoveSyntaxTreesFromReferences(this Project project)
    {
        var solution = project.Solution;
        var projectReferencesCompilationTasks = project.ProjectReferences
            .Select(o => solution.GetProject(o.ProjectId)!.GetCompilationAsync())
            .ToArray();

        var compilations = await Task.WhenAll(projectReferencesCompilationTasks);
        var assembliesAsBytes = compilations
            .Select(o =>
            {
                var memoryStream = new MemoryStream();
                o!.Emit(memoryStream);
                return memoryStream.ToArray();
            })
            .ToArray();

        var metadataReferences = assembliesAsBytes
            .Select(o => MetadataReference.CreateFromImage(o))
            .ToArray();

        project = project
            .WithProjectReferences(Enumerable.Empty<ProjectReference>())
            .AddMetadataReferences(metadataReferences);

        return project;
    }

    public static async Task<Diagnostic[]> ApplyAnalyzers(this Project project)
    {
        var compilation = await project.GetCompilationAsync();
        var analyzerResults = await compilation!
            .WithAnalyzers(Analyzers)
            .GetAllDiagnosticsAsync();

        var error = compilation!.GetDiagnostics()
            .Concat(analyzerResults);

        return error.ToArray();
    }

    
    public static SettingsTask Track(this SettingsTask settingsTask, string value, [CallerArgumentExpression("value")]string name = null!)
    {
        settingsTask.AddScrubber(o => o.Replace(value, name));
        return settingsTask;
    }
}