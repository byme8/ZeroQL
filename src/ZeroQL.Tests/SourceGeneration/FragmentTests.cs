using FluentAssertions;
using Xunit;
using ZeroQL.Core;
using ZeroQL.TestApp.Models;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class FragmentTests: IntegrationTest
{
    [Fact]
    public async Task CanCreateClassInstance()
    {
        var csharpQuery = "static q => q.Me(o => new UserModal(o.FirstName, o.LastName, o.Role(o => o.Name)))";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var response = (GraphQLResult<UserModal>)await project.Validate(graphqlQuery);
        
        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }
    
    [Fact]
    public async Task CanApplyFragment()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserWithRoleName())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var response = (GraphQLResult<UserModal>)await project.Validate(graphqlQuery);
        
        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }

}