using CliFx.Infrastructure;
using ZeroQL.CLI.Commands;
using ZeroQL.Tests.Core;

namespace ZeroQL.Tests.Tools;

public class Tools : IntegrationTest
{
    [Fact]
    public async Task ExtractGraphQLFile()
    {
        var httpClient = new HttpClient();
        var graphql = await httpClient.GetStringAsync("http://localhost:10000/graphql?sdl");
        await File.WriteAllTextAsync("../../../../TestApp/ZeroQL.TestApp/schema.graphql", graphql);
        
        var console = new FakeConsole();
        var generateCommand = new GenerateCommand
        {
            Schema = "../../../../TestApp/ZeroQL.TestApp/schema.graphql",
            Namespace = "GraphQL.TestServer",
            ClientName = "TestServerClient",
            Output = "../../../../TestApp/ZeroQL.TestApp/Generated/GraphQL.g.cs",
            Force = true
        };

        await generateCommand.ExecuteAsync(console);
    }
}