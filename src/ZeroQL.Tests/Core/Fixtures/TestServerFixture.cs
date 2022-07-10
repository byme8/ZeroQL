using ZeroQL.TestServer;
using Xunit;

namespace ZeroQL.Tests.Core.Fixtures;

public class TestServerFixture : IAsyncLifetime
{
    private int count;

    public async Task InitializeAsync()
    {
        if (Interlocked.CompareExchange(ref count, 0, 1) != 1)
        {
            var server = Program.StartServer(Array.Empty<string>());
        }

        if (!await Program.VerifyServiceIsRunning())
        {
            throw new InvalidOperationException("Server failed to bootstrap");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}