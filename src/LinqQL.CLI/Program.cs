using CliFx;
using LinqQL.CLI.Commands;

await new CliApplicationBuilder()
    .AddCommand<GenerateCommand>()
    .Build()
    .RunAsync();