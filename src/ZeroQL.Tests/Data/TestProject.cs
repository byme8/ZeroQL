using System.Reflection;
using Buildalyzer;
using Buildalyzer.Workspaces;
using FluentAssertions;
using ZeroQL.Tests.Core;
using Microsoft.CodeAnalysis;

namespace ZeroQL.Tests.Data;

public static class TestProject
{
    public const string PLACE_TO_REPLACE = "// place to replace";
    public const string ME_QUERY = @"static q => q.Me(o => o.FirstName)";
    public const string FULL_ME_QUERY = @"Query(static q => q.Me(o => o.FirstName))";
    public const string FULL_CALL = "await qlClient.Query(static q => q.Me(o => o.FirstName));";
    public const string FULL_LINE = "var response = await qlClient.Query(static q => q.Me(o => o.FirstName));";

    static TestProject()
    {
        var manager = new AnalyzerManager();
        manager.GetProject(@"../../../../TestApp/ZeroQL.TestApp/ZeroQL.TestApp.csproj");
        Workspace = manager.GetWorkspace();

        Project = Workspace.CurrentSolution.Projects.First(o => o.Name == "ZeroQL.TestApp");
    }

    public static Project Project { get; }

    public static AdhocWorkspace Workspace { get; }

    public static async Task<IGraphQLResult> ExecuteRequest(this Assembly assembly, CancellationToken token = default)
    {
        var method = (assembly.GetType("ZeroQL.TestApp.Program")!
                .GetMethod("Execute", BindingFlags.Static | BindingFlags.Public)!
                .CreateDelegate(typeof(Func<CancellationToken, Task<IGraphQLResult>>)) as
            Func<CancellationToken, Task<IGraphQLResult>>)!;

        return await method.Invoke(token);
    }

    public static async Task<IGraphQLResult> Execute(this Project project, CancellationToken token = default)
    {
        project = await project.RemoveSyntaxTreesFromReferences();
        var assembly = await project.CompileToRealAssembly();
        var response = await assembly.ExecuteRequest(token);

        return response;
    }

    public static async Task<IGraphQLResult> Validate(this Project project, string? graphqlQuery,
        bool checkError = true)
    {
        project = await project.RemoveSyntaxTreesFromReferences();
        var assembly = await project.CompileToRealAssembly();
        var response = await assembly.ExecuteRequest();

        response.Query.Should().Be(graphqlQuery);

        if (checkError)
        {
            response.Errors.Should().BeNull();
        }

        return response;
    }
}