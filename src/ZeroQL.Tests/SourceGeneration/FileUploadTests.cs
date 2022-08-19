using FluentAssertions;
using Xunit;
using ZeroQL.Core;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class FileUploadTests : IntegrationTest
{
    [Fact]
    public async Task UploadFileWithOtherArgument()
    {
        var csharpQuery = "Mutation(new AddProfileImage(1, new Upload(\"image.png\", new MemoryStream(new byte[42]))), static (i, m) => m.AddUserProfileImage(i.UserId, i.File))";
        var graphqlQuery = @"mutation ($userId: Int!, $file: Upload!) { addUserProfileImage(userId: $userId, file: $file)}";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullMeQuery, csharpQuery));

        var result = (GraphQLResult<int>)await project.Validate(graphqlQuery);
        result.Data.Should().Be(42);
    }
    
    [Fact]
    public async Task UploadSingleFile()
    {
        var csharpQuery = "Mutation(new { File = new Upload(\"image.png\", new MemoryStream(new byte[42])) }, static (i, m) => m.AddMyProfileImage(i.File))";
        var graphqlQuery = @"mutation ($file: Upload!) { addMyProfileImage(file: $file)}";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullMeQuery, csharpQuery));

        var result = (GraphQLResult<int>)await project.Validate(graphqlQuery);
        result.Data.Should().Be(42);
    }
}