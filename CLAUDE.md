# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ZeroQL is a high-performance, C#-friendly GraphQL client that uses source generators to create strongly-typed clients at compile time. It provides a LINQ-like syntax for GraphQL queries while maintaining performance close to raw HTTP calls.

## Key Commands

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run specific test project
dotnet test src/ZeroQL.Tests/ZeroQL.Tests.csproj

# Pack NuGet packages (PowerShell)
./pack.ps1 -version <version>

# CLI operations (after installing: dotnet tool install ZeroQL.CLI)
dotnet zeroql schema pull --url <graphql-endpoint>  # Pull schema from GraphQL endpoint
dotnet zeroql config init                           # Initialize configuration
dotnet zeroql generate                              # Generate client from config
dotnet zeroql queries extract                       # Extract queries from code
```

## Architecture

The codebase is organized into these key components:

- **ZeroQL.Core**: Core types and configuration models
- **ZeroQL.Runtime**: Runtime execution engine for GraphQL queries, including client extensions and JSON serialization
- **ZeroQL.SourceGenerators**: C# source generators that analyze lambda expressions and generate GraphQL queries at compile time
- **ZeroQL.CLI**: Command-line tool for schema pulling and code generation
- **ZeroQL.Tools**: Shared GraphQL schema parsing and code generation utilities
- **ZeroQL.MSBuild**: MSBuild tasks for automatic generation during build
- **ZeroQL.Package**: NuGet package that bundles all components

## Key Development Patterns

1. **Source Generation Flow**: User code with lambda expressions → Source generators analyze at compile time → Generated GraphQL queries in `./obj/ZeroQL`

2. **Testing**: Uses xUnit with Verify for snapshot testing. Generated code is verified against `.verified.txt` files.

3. **Multi-targeting**: Supports .NET Standard 2.0, .NET 6.0, 7.0, 8.0, and 9.0. Use conditional compilation when needed.

4. **Configuration**: Projects use `*.zeroql.json` files for configuration with JSON schema validation.

5. **Performance Focus**: Avoid runtime reflection, prefer compile-time generation. Benchmark critical paths using the Benchmarks project.

## Working with Source Generators

When modifying source generators:
1. Changes in `ZeroQL.SourceGenerators` require rebuilding consumer projects to see effects
2. Use incremental generators pattern for better IDE performance
3. Test generated code using snapshot tests in `ZeroQL.Tests/SourceGeneration`

## GraphQL Client Usage Patterns

The library supports two syntax styles:
- **Lambda syntax**: `client.Query(q => q.User(42, o => new { o.Id, o.Name }))`
- **Request syntax**: Being deprecated in favor of lambda syntax

## Shortcut Commands

Here would be a list of commands that can be used in promt. Pay attention to them and activate appropriate flow when neccesary

### Task Management
> **IHNT**: "I have new task" - When you see this, pause coding and analyze the task first.
> Follow these steps:
> 1. Think through the task requirements
> 2. Write a detailed description of what needs to be done
> 3. Create a step-by-step plan for implementation
> 4. Ask clarifying questions if needed
> 5. Document your analysis in `claude/<task_name>.md` for reference