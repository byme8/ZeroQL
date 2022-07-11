using Xunit;
using ZeroQL.Tests.Core.Fixtures;

namespace ZeroQL.Tests.Core;

[Collection("Integration")]
public class IntegrationTest
    : IClassFixture<TestServerFixture>
{
}