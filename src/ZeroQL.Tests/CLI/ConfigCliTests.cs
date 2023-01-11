using CliFx.Infrastructure;
using ZeroQL.CLI.Commands;

namespace ZeroQL.Tests.CLI;

[UsesVerify]
public class ConfigCliTests
{
    [Fact]
    public async Task CanCreateWithDefaultValues()
    {
        using var console = new FakeInMemoryConsole();
        var tempFile = Path.GetTempFileName();
        var command = new InitConfigCommand()
        {
            Output = tempFile
        };

        await command.ExecuteAsync(console);
        
        var config = await File.ReadAllTextAsync(tempFile);
        
        await Verify(config);
    }
    
    [Fact]
    public async Task CanCreateWithCustomValues()
    {
        using var console = new FakeInMemoryConsole();
        console.WriteInput($"""
                    ./service.graphql
                    Service.ZeroQL.Client
                    ServiceZeroQLClient
                    QL.g.cs
                    """);
        
        var tempFile = Path.GetTempFileName();
        var command = new InitConfigCommand()
        {
            Output = tempFile
        };

        await command.ExecuteAsync(console);
        
        var config = await File.ReadAllTextAsync(tempFile);
        
        await Verify(config);
    }
}