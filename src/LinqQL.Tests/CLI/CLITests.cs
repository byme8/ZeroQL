using CliFx.Infrastructure;
using LinqQL.CLI.Commands;
using Xunit;

namespace LinqQL.Tests.CLI
{
    public class CLITests
    {
        [Fact]
        public async Task Test()
        {
            using var console = new FakeInMemoryConsole();
            var generateCommand = new GenerateCommand();
            generateCommand.Project = "../../../../LinqQL.TestApp/LinqQL.TestApp.csproj";

            await generateCommand.ExecuteAsync(console);
        }
    }
}