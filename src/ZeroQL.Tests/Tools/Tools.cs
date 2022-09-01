using Xunit;
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
    }
}