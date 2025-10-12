using ZeroQL.Tests.Core.Fixtures;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ZeroQL.Tests.Core;

[Collection("Integration")]
public class IntegrationTest
    : IClassFixture<TestServerFixture>
{
}