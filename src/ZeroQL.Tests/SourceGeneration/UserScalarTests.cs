using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

[UsesVerify]
public class UserScalarTests: IntegrationTest
{
    [Fact]
    public async Task InstantScalarTypeWorks()
    {
        var csharpQuery = "static q => q.Instant";
        var graphqlQuery = @"query { instant}";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (TestProject.PLACE_TO_REPLACE, "global::ZeroQL.Json.ZeroQLJsonOptions.Configure(o => o.Converters.Add(new InstantJsonConverter()));"),
                (TestProject.ME_QUERY, csharpQuery));

        var response = await project.Validate(graphqlQuery, false);

        await Verify(response);
    }
    
}