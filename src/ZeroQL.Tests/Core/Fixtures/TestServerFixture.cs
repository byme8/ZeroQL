using ZeroQL.TestServer;

namespace ZeroQL.Tests.Core.Fixtures;

public class TestServerFixture : IAsyncLifetime
{
    private int count;

    public Program.ServerContext? Context { get; set; }

    public async Task InitializeAsync()
    {
        if (Interlocked.CompareExchange(ref count, 0, 1) != 1)
        {
            Context = new Program.ServerContext()
            {
                Arguments = Array.Empty<string>(),
                Port = 10000
            };

            var server = Program.StartServer(Context);
        }

        if (!await Program.VerifyServiceIsRunning(Context!))
        {
            throw new InvalidOperationException("Server failed to bootstrap");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}