using LinqQL.Tests.Core;
using Xunit;

namespace LinqQL.Tests;

public class Tools : IntegrationTest
{
    [Fact]
    public async Task ExtractGraphQLFile()
    {
        var httpClient = new HttpClient();
        var graphql = await httpClient.GetStringAsync("http://localhost:10000/graphql?sdl");
        await File.WriteAllTextAsync("../../../Data/TestServer.graphql", graphql);
        await File.WriteAllTextAsync("../../../../LinqQL.TestApp/schema.graphql", graphql);
    }
}