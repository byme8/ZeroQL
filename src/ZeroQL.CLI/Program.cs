using CliFx;
using ZeroQL.CLI.Commands;

await new CliApplicationBuilder()
    .AddCommand<GenerateCommand>()
    .Build()
    .RunAsync();