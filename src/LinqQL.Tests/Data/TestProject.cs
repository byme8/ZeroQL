using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;

namespace LinqQL.Tests.Data;

public static class TestProject
{
    static TestProject()
    {
        var manager = new AnalyzerManager();
        manager.GetProject(@"../../../../LinqQL.TestApp/LinqQL.TestApp.csproj");
        Workspace = manager.GetWorkspace();

        Project = Workspace.CurrentSolution.Projects.First(o => o.Name == "LinqQL.TestApp");
    }

    public static Project Project { get; }

    public static AdhocWorkspace Workspace { get; }
}