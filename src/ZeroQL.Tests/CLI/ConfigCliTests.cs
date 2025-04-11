using CliFx.Infrastructure;
using ZeroQL.CLI.Commands;
using ZeroQL.Tests.Core;

namespace ZeroQL.Tests.CLI;

public class ConfigCliTests
{
    [Fact]
    public async Task CanCreateWithDefaultValues()
    {
        using var console = new FakeInMemoryConsole();
        var tempFile = Path.GetTempFileName();
        var command = new ConfigInitCommand()
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
                    """);

        var tempFile = Path.GetTempFileName();
        var command = new ConfigInitCommand()
        {
            Output = tempFile
        };

        await command.ExecuteAsync(console);

        var config = await File.ReadAllTextAsync(tempFile);

        await Verify(config);
    }

    [Fact]
    public async Task ConfigCanBeParsed()
    {
        using var console = new FakeInMemoryConsole();
        var tempFile = Path.GetTempFileName();
        var config = """
            {
              "graphql": "./service.graphql",
              "namespace": "Service.ZeroQL.Client",
              "clientName": "ServiceZeroQLClient",
              "visibility": "Internal",
              "output": "QL.g.cs",
              "warningsToIgnore": ["CS0168", "CS0219"],
              "schemaDownloadTimeoutInSeconds": 60,
              "scalars": {
                "Point": "Geometry.Point",
                "Rect": "Geometry.Rect"
              }
            }
            """;

        await File.WriteAllTextAsync(tempFile, config);

        var generateCommand = new GenerateCommand
        {
            Config = tempFile,
        };

        await generateCommand.ReadConfig(console);

        await Verify(generateCommand)
            .Track(tempFile);
    }

    [Fact]
    public async Task ConfigWithUnrecognizedFieldFails()
    {
        using var console = new FakeInMemoryConsole();
        var tempFile = Path.GetTempFileName();
        var config = """
            {
              "graphql": "./service.graphql",
              "namespace": "Service.ZeroQL.Client",
              "clientName": "ServiceZeroQLClient",
              "something": "else",
              "visibility": "Internal",
              "output": "QL.g.cs",
              "scalars": {
                "Point": "Geometry.Point",
                "Rect": "Geometry.Rect"
              }
            }
            """;

        await File.WriteAllTextAsync(tempFile, config);

        var generateCommand = new GenerateCommand
        {
            Config = tempFile,
        };

        await generateCommand.ReadConfig(console);

        await Verify(console.ReadErrorString())
            .Track(tempFile);
    }

    [Fact]
    public async Task EchoOutput()
    {
        using var console = new FakeInMemoryConsole();
        var tempFile = Path.GetTempFileName();
        var config = """
                     {
                       "graphql": "./service.graphql",
                       "namespace": "Service.ZeroQL.Client",
                       "clientName": "ServiceZeroQLClient",
                       "output": "QL.g.cs",
                     }
                     """;

        await File.WriteAllTextAsync(tempFile, config);

        var generateCommand = new ConfigEchoOutputCommand
        {
            Config = tempFile
        };

        await generateCommand.ExecuteAsync(console);

        await Verify(console.ReadOutputString())
            .Track(tempFile);
    }
}