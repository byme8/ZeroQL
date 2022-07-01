using CliFx;

await new CliApplicationBuilder()
    .AddCommandsFromThisAssembly()
    .Build()
    .RunAsync();