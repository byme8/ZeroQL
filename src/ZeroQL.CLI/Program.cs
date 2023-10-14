using CliFx;
using ZeroQL.CLI.Commands;

await new CliApplicationBuilder()
    .AddCommand<ConfigInitCommand>()
    .AddCommand<ConfigEchoOutputCommand>()
    .AddCommand<GenerateCommand>()
    .AddCommand<ExtractQueriesCommand>()
    .AddCommand<PullSchemaCommand>()
    .Build()
    .RunAsync();