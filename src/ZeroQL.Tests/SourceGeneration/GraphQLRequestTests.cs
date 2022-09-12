using FluentAssertions;
using Xunit;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class GraphQLRequestTests : IntegrationTest
{
    public const string FULL_CALL = "await qlClient.Query(static q => q.Me(o => o.FirstName));";
    
    [Fact]
    public async Task CanSendRequestLikeQuery()
    {
        var csharpQuery = "await qlClient.Execute(new GetUserById(1));";
        var graphqlQuery = "query GetUserById($id: Int!) { user(id: $id) { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (FULL_CALL, csharpQuery));

        await project.Validate(graphqlQuery);
    } 
    
    [Fact]
    public async Task CanSendRequestLikeQueryWithExpression()
    {
        var csharpQuery = "await qlClient.Execute(new GetUserByIdExpression(1));";
        var graphqlQuery = "query GetUserByIdExpression($id: Int!) { user(id: $id) { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (FULL_CALL, csharpQuery));

        await project.Validate(graphqlQuery);
    }
    
    [Fact]
    public async Task CanSendRequestLikeMutation()
    {
        var csharpQuery = "await qlClient.Execute(new AddUser(\"John\", \"Smith\"));";
        var graphqlQuery = "mutation AddUser($firstName: String!, $lastName: String!) { addUser(firstName: $firstName, lastName: $lastName) { id } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (FULL_CALL, csharpQuery));

        dynamic response = await project.Validate(graphqlQuery);
        int id = response.Data;

        id.Should().Be(10);
    }
    
    [Fact]
    public async Task CanSendRequestLikeMutationWithUpload()
    {
        var csharpQuery = "await qlClient.Execute(new AddAvatar(1, new Upload(\"image.png\", new MemoryStream(new byte[42]))));";
        var graphqlQuery = "mutation AddAvatar($userId: Int!, $file: Upload!) { addUserProfileImage(userId: $userId, file: $file)}";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (FULL_CALL, csharpQuery));

        dynamic response = await project.Validate(graphqlQuery);
        int id = response.Data;

        id.Should().Be(42);
    }
}