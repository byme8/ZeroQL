using System.Reflection;
using Buildalyzer;
using Buildalyzer.Workspaces;
using FluentAssertions;
using ZeroQL.Tests.Core;
using Microsoft.CodeAnalysis;

namespace ZeroQL.Tests.Data;

public static class TestProject
{
    public const string PlaceToReplace = "// place to replace";
    public const string PlaceToReplaceInClassProgram = "// class Program place to replace in ";
    public const string MeQuery = @"static q => q.Me(o => o.FirstName)";
    public const string FullMeQuery = @"Query(static q => q.Me(o => o.FirstName))";
    public const string FullCall = "await qlClient.Query(static q => q.Me(o => o.FirstName));";
    public const string FullLine = "var response = await qlClient.Query(static q => q.Me(o => o.FirstName));";

    static TestProject()
    {
        var manager = new AnalyzerManager();
        manager.GetProject(@"../../../../TestApp/ZeroQL.TestApp/ZeroQL.TestApp.csproj");
        Workspace = manager.GetWorkspace();

        Project = Workspace.CurrentSolution.Projects.First(o => o.Name == "ZeroQL.TestApp");
    }

    public static Project Project { get; }

    public static AdhocWorkspace Workspace { get; }

    public static async Task<object> Execute(this Assembly assembly, CancellationToken token = default)
    {
        var method = (assembly.GetType("ZeroQL.TestApp.Program")!
                .GetMethod("Execute", BindingFlags.Static | BindingFlags.Public)!
                .CreateDelegate(typeof(Func<CancellationToken, Task<object>>)) as
            Func<CancellationToken, Task<object>>)!;

        var response = await method.Invoke(token);

        return response;
    }

    public static async Task<object> Execute(this Project project, CancellationToken token = default)
    {
        project = await project.RemoveSyntaxTreesFromReferences();
        var assembly = await project.CompileToRealAssembly();
        var response = await assembly.Execute(token);

        return response;
    }
    
    public static async Task<object> Execute(this Project project, string csharpQuery)
    {
        project = await project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        return await project.Execute();
    }

    public static async Task<IGraphQLResult> Validate(this Project project, string? graphqlQuery,
        bool checkError = true)
    {
        project = await project.RemoveSyntaxTreesFromReferences();
        var assembly = await project.CompileToRealAssembly();
        var response = await assembly.Execute();
        var queryResponse = (IGraphQLResult)response;

        queryResponse.Query.Should().Be(graphqlQuery);

        if (checkError)
        {
            queryResponse.Errors.Should().BeNull();
        }

        return queryResponse;
    }
}