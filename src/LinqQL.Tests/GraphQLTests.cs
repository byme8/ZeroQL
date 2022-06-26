using LinqQL.Core;
using LinqQL.TestServer;
using LinqQL.Tests.Core;
using LinqQL.Tests.Data;
using Xunit;

namespace LinqQL.Tests;

public class GraphQLTests : IntegrationTest
{
    public GraphQLTests()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(Program.TEST_SERVER_URL);

        Client = new GraphQLClient(httpClient);

    }

    public GraphQLClient Client
    {
        get;
    }

    [Fact]
    public async Task ServerWorks()
    {
        var query = @"{""query"":""query ($id: Int!) { user (id: $id) { \n  firstName\n  lastName\n }}"",""variables"":{""id"":42}}";

        var user = await Client.Execute<TestUser>(query);
        
        Assert.Equal("Jon", user.FirstName);
        Assert.Equal("Smith", user.LastName);
    }
}