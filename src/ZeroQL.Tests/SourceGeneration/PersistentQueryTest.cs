using FluentAssertions;
using Xunit;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class PersistentQueryTest : IntegrationTest
{
    [Fact]
    public async Task CanSendAutomaticQuery()
    {
        var query = "8ed4d3e773b6f87d986cc128a716cfc85d030c5fe6a5b585ab0c0820ac5d9728:query { me { firstName } }";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (
                    "var qlClient = new TestServerClient(httpClient);",
                    "var qlClient = new TestServerClient(httpClient, new PersistentQueryStrategy());"
                ));

        await project.Validate(query);
    }
}