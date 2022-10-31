using GraphQL.TestServer;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using ZeroQL.Tests.HotChocolateIntegration;
using ZeroQL.TestServer;

namespace ZeroQL.Tests.SourceGeneration;

public class TransportTests
{
    [Fact]
    public async Task SimpleQuery_HotChocolate_Without_Http_Endpoint()
    {
        //Setup HotChocolate pipeline
        var serviceCollection = new ServiceCollection();
        var requestExecutorBuilder = Program.AddBasicGraphQLServices(serviceCollection);
        IRequestExecutor executor = await requestExecutorBuilder.BuildRequestExecutorAsync();

        var zeroQLClient = new TestServerClient(new HotChocoTransport(executor));
        var firstname = await zeroQLClient.Query(static q => q.Me(o => o.FirstName));

        Assert.Null(firstname.Errors);
        Assert.Equal("Jon", firstname.Data);
    }

}
