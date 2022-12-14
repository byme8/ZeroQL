using FluentAssertions;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;
using static ZeroQL.Tests.Data.TestProject;

namespace ZeroQL.Tests.SourceGeneration;

[UsesVerify]
public class CancellationTokenTests : IntegrationTest
{
    [Fact]
    public async Task QueryWithCancellationToken()
    {
        var csharpQuery = "static q => q.Me(o => o.FirstName), cancellationToken";

        var project = await Project
            .ReplacePartOfDocumentAsync("Program.cs", (ME_QUERY, csharpQuery));

        var cancellationToken = new CancellationTokenSource();
        var response = await project.Execute(cancellationToken.Token);

        await Verify(response);
    }
    
    [Fact]
    public async Task QueryWithCanceledCancellationToken()
    {
        var csharpQuery = "static q => q.LongOperation, cancellationToken";

        var project = await Project
            .ReplacePartOfDocumentAsync("Program.cs", (ME_QUERY, csharpQuery));

        var cancellationToken = new CancellationTokenSource();
        var response = project.Execute(cancellationToken.Token);
        await Task.Delay(100);
        
        cancellationToken.Cancel();
        
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await response);

        await Verify(new
        {
            response.IsCompleted,
            response.IsCanceled,
            response.Exception?.Message,
        });
    }
    
    [Fact]
    public async Task MutationWithCancellationToken()
    {
        var csharpQuery = "Mutation(static m => m.AddUser(\"Jon\", \"Smith\", o => o.FirstName), cancellationToken)";

        var project = await Project
            .ReplacePartOfDocumentAsync("Program.cs", (FULL_ME_QUERY, csharpQuery));

        var cancellationToken = new CancellationTokenSource();
        var response = await project.Execute(cancellationToken.Token);

        await Verify(response);
    }
    
    [Fact]
    public async Task RequestWithCancellationToken()
    {
        var csharpQuery = "await qlClient.Execute(new GetUserById(1), cancellationToken);";

        var project = await Project
            .ReplacePartOfDocumentAsync("Program.cs", (FULL_CALL, csharpQuery));

        var cancellationToken = new CancellationTokenSource();
        var response = await project.Execute(cancellationToken.Token);

        await Verify(response);
    } 
}