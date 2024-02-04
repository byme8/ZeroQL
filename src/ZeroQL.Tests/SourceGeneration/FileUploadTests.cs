using FluentAssertions;
using ZeroQL.SourceGenerators.Generator;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

[UsesVerify]
public class FileUploadTests : IntegrationTest
{
    [Fact]
    public async Task UploadFileGenerates()
    {
        var usersVariable = @"var users = new UserInfoInput[]
        {
            new() { FirstName = ""John"", LastName = ""Smith"", Avatar = new Upload(""image.png"", new MemoryStream(new byte[42])) },
            new() { FirstName = ""Ben"", LastName = ""Smith"", Avatar = new Upload(""image.png"", new MemoryStream(new byte[42])) }
        };";
        var csharpQuery = "Mutation(new { Users = users }, static (i, m) => m.AddUsersInfo(i.Users))";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (TestProject.PlaceToReplace, usersVariable), 
                (TestProject.FullMeQuery, csharpQuery));

        var result = await project.ApplyGenerator(new GraphQLLambdaIncrementalSourceGenerator());
       
        await Verify(result.GeneratedTrees.Select(o => o.ToString()));
    }
    
    [Fact]
    public async Task UploadFileAsClassInstance()
    {
        var csharpQuery = "Mutation(new AddProfileImage(1, new Upload(\"image.png\", new MemoryStream(new byte[42]))), static (i, m) => m.AddUserProfileImage(i.UserId, i.File))";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullMeQuery, csharpQuery));

        var result = (GraphQLResult<int>)await project.Execute();
        result.Data.Should().Be(42);
    }
    
    [Fact]
    public async Task UploadFileAsAnonymousTypeWithMultipleProperties()
    {
        var csharpQuery = "Mutation(new { UserId = 1, File = new Upload(\"image.png\", new MemoryStream(new byte[42])) }, static (i, m) => m.AddUserProfileImage(i.UserId, i.File))";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullMeQuery, csharpQuery));

        var result = await project.Execute();

        await Verify(result);
    }

    [Fact]
    public async Task UploadFileAsAnonymousType()
    {
        var csharpQuery = "Mutation(new { File = new Upload(\"image.png\", new MemoryStream(new byte[42])) }, static (i, m) => m.AddMyProfileImage(i.File))";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullMeQuery, csharpQuery));

        var result = await project.Execute();

        await Verify(result);
    }

    [Fact]
    public async Task UploadFileAsDeepNestedAnonymousType()
    {
        var usersVariable = @"var users = new UserInfoInput[]
        {
            new() { FirstName = ""John"", LastName = ""Smith"", Avatar = new Upload(""image.png"", new MemoryStream(new byte[42])) },
            new() { FirstName = ""Ben"", LastName = ""Smith"", Avatar = new Upload(""image.png"", new MemoryStream(new byte[42])) }
        };";
        var csharpQuery = "Mutation(new { Users = users }, static (i, m) => m.AddUsersInfo(i.Users))";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (TestProject.PlaceToReplace, usersVariable), 
                (TestProject.FullMeQuery, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }
    
    [Fact]
    public async Task UploadFileAsDeepNestedAnonymousTypeWithClosureSyntax()
    {
        var usersVariable = @"var users = new UserInfoInput[]
        {
            new() { FirstName = ""John"", LastName = ""Smith"", Avatar = new Upload(""image.png"", new MemoryStream(new byte[42])) },
            new() { FirstName = ""Ben"", LastName = ""Smith"", Avatar = new Upload(""image.png"", new MemoryStream(new byte[42])) }
        };";
        var csharpQuery = "Mutation(m => m.AddUsersInfo(users))";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (TestProject.PlaceToReplace, usersVariable), 
                (TestProject.FullMeQuery, csharpQuery));

        var result = await project.Execute();

        await Verify(result);
    }
    
    [Fact]
    public async Task UploadFileWhenValueIsNull()
    {
        var csharpQuery = """
            var input = new AddUsersInput();
            var response = await qlClient.Mutation(m => m.AddUsersInfoWithEmails(input));
        """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var result = (GraphQLResult<int>)await project.Execute();
        result.Data.Should().Be(10);
    }
}