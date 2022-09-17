using CliFx;
using ZeroQL.CLI.Commands;

await new CliApplicationBuilder()
    .AddCommand<GenerateCommand>()
    .AddCommand<ExtractQueriesCommand>()
    .Build()
    .RunAsync();