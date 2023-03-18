using CliFx;
using ZeroQL.CLI.Commands;

await new CliApplicationBuilder()
    .AddCommand<GenerateCommand>()
    .AddCommand<ExtractQueriesCommand>()
    .AddCommand<InitConfigCommand>()
    .AddCommand<PullSchemaCommand>()
    .Build()
    .RunAsync();