# ZeroQL Usage Guide

## Build & Test Commands
- Build solution: `dotnet build`
- Run all tests: `dotnet test`
- Run specific test: `dotnet test --filter "FullyQualifiedName=ZeroQL.Tests.<TestClass>.<TestMethod>"`
- CLI Tool: `dotnet zeroql schema pull --url <graphql-endpoint>`

## Code Style Guidelines
- **Naming**: PascalCase for classes, methods, properties; camelCase for parameters
- **Abbreviations**: "ID" and "QL" remain uppercase (e.g., GraphQL, UserID)
- **Formatting**: 4-space indentation, braces required for all control statements
- **Types**: Prefer `var` over concrete types
- **Error Handling**: Prefer `Result<T>` pattern over exceptions for expected errors
- **Testing**: Use xUnit with Verify for snapshot tests
- **Source Generation**: Prefer source generators over reflection for performance
- **GraphQL Style**: Use LINQ-like syntax for queries, organize fragments in separate files
- **Git**: Before commit verify that git has files to commit

## Project Structure
ZeroQL is a high-performance C# GraphQL client using source generation and LINQ-like syntax for type-safe queries.