using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class WrapperTests : IntegrationTest
{
    [Fact]
    public async Task WrapperWorks()
    {
        var csharpQuery = """
            var wrapper = new Services.GraphQLClientWrapper(qlClient);
            var response = await wrapper.QueryAsync(q => q.Me(o => o.FirstName));
            """;
        
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));
        
        var result = await project.Execute();
        
        await Verify(result);
    }
    
    [Fact]
    public async Task MethodWrapperWorks()
    {
        var csharpQuery = """
                          var response = await Services.GraphQLClientMethodWrapper.MakeQuery(qlClient, q => q.Me(o => o.FirstName));
                          """;
        
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var result = await project.Execute();
        
        await Verify(result);
    }
    
    [Fact]
    public async Task LocalMethodWrapperWorks()
    {
        var csharpQuery = """
                          var response = await MakeQuery(qlClient, q => q.Me(o => o.FirstName));
                          """;
        
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var result = await project.Execute();
        
        await Verify(result);
    }

    [Fact]
    public async Task HaveDashInAssemblyName()
    {
        var csharpQuery = """
                          var response = await MakeQuery(qlClient, q => q.Me(o => o.FirstName));
                          """;

        var project = await TestProject.Project
            .WithAssemblyName("some-dash-name")
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var result = await project.Execute();

        await Verify(result);
    }
}