using FluentAssertions;
using Microsoft.CodeAnalysis;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class FragmentTests : IntegrationTest
{
    [Fact]
    public async Task CanCreateClassInstance()
    {
        var csharpQuery = "static q => q.Me(o => new UserModel(o.FirstName, o.LastName, o.Role(o => o.Name)))";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await Validate(project, graphqlQuery);
    }

    [Fact]
    public async Task CanApplyFragmentWithBody()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserWithRoleNameBody())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await Validate(project, graphqlQuery);
    }

    [Fact]
    public async Task CanApplyFragmentWithExpression()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserWithRoleNameExpression())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await Validate(project, graphqlQuery);
    }
    
    [Fact]
    public async Task CanApplyFragmentWithInitializers()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserWithRoleNameInitializers())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await Validate(project, graphqlQuery);
    }
    
    [Fact]
    public async Task CanApplyFragmentWithEnumCast()
    {
        var csharpQuery = "q => q.Me(o => o.AsUserRead())";
        var result = await TestProject.Project.Execute(csharpQuery);

        await Verify(result);
    }
    
    [Fact]
    public async Task CanApplyFragmentWithArgument()
    {
        var csharpQuery = """
          var id = 1;
          var response = await qlClient.Query(q => q.GetUserById(id));
      """;

        var response = await TestProject.Project.ExecuteFullLine(csharpQuery);
        
        await Verify(response);
    }
    
    [Fact]
    public async Task CanApplyFragmentWithConstantArgument()
    {
        var csharpQuery = "static q => q.GetUserById(1)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }
    
    [Fact]
    public async Task CanApplyFragmentWithClosureLambda()
    {
        var csharpQuery = """
            var userId = 1;
            var response = await qlClient.Query(q => q.GetUserById(userId));
        """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }

    [Fact]
    public async Task CanLoadFragmentFromDifferentProject()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserFromDifferentAssembly())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await Validate(project, graphqlQuery);
    }
    
    [Fact]
    public async Task CanLoadFragmentFromDifferentProjectWitharguments()
    {
        var csharpQuery = """
                              var id = 1;
                              var response = await qlClient.Query(q => q.AsUserFromDifferentAssembly(id));
                          """;

        var response = await TestProject.Project.ExecuteFullLine(csharpQuery);
        
        await Verify(response);
    }    
    
    [Fact]
    public async Task FragmentsWithPartialKeywordIsExtended()
    {
        var csharpQuery = "static q => q.Me(o => o.ExposedFragmentUserWithRole())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await Validate(project, graphqlQuery);
    }
    
    [Fact]
    public async Task FragmentsWithPartialKeywordWithoutNamespaceIsExtended()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserWithoutNamespace())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await Validate(project, graphqlQuery);
    }

    private static async Task Validate(Project project, string graphqlQuery)
    {
        dynamic response = await project.Validate(graphqlQuery);

        ((string)response.Data.FirstName).Should().Be("Jon");
        ((string)response.Data.LastName).Should().Be("Smith");
        ((string)response.Data.Role).Should().Be("Admin");
    }
}