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
            generateCommand.Schema = "../../../../LinqQL.TestApp/schema.graphql";
            generateCommand.Namespace = "GraphQL.TestServer";
            generateCommand.QueryName = "TestServerQuery";
            generateCommand.Output = "../../../../LinqQL.TestApp/Generated/GraphQL.g.cs";

            await generateCommand.ExecuteAsync(console);
        }
    }
}