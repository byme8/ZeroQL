# ZeroQL | [![GitHub](https://img.shields.io/github/license/byme8/ZeroQL?color=blue&style=flat-square)](https://github.com/byme8/ZeroQL/blob/main/LICENCE) [![Nuget](https://img.shields.io/nuget/v/zeroql?color=blue&style=flat-square)](https://www.nuget.org/packages/ZeroQL) [![.NET](https://github.com/byme8/ZeroQL/actions/workflows/dotnet.yml/badge.svg)](https://github.com/byme8/ZeroQL/actions/workflows/dotnet.yml)

🚀 Welcome to ZeroQL, a high-performance C#-friendly GraphQL client! 🎉

ZeroQL makes it easy to perform queries and mutations with Linq-like syntax. Unlike other GraphQL clients, ZeroQL doesn't require Reflection.Emit or expressions, which means the runtime provides performance very close to a raw HTTP call.

# Features

Here's a quick rundown of what ZeroQL can do at the moment:
- [x] Bootstrap schema.graphql file from graphql endpoint
- [x] Bootstrap client from schema.graphql file
- [x] Support for queries and mutations
    - [x] ["Lambda" like syntax](#graphql-lambda-syntax)
    - [x] ["Request" like syntax](#graphql-request-syntax)
- [ ] Support for subscriptions
- [x] [Support for fragments](https://github.com/byme8/ZeroQL/wiki/Fragments)
- [x] [Support for interfaces](https://github.com/byme8/ZeroQL/wiki/Interfaces)
- [x] [Support for unions](https://github.com/byme8/ZeroQL/wiki/Unions)
- [x] [Support for file uploads](https://github.com/byme8/ZeroQL/wiki/File-upload)
- [x] [Support for persisted queries](https://github.com/byme8/ZeroQL/wiki/Persisted-queries)
- [ ] Support for @defer
- [ ] Support for @stream

Check out our articles to learn more about ZeroQL:
- [ZeroQL - C# friendly graphql client](https://dev.to/byme8/zeroql-c-friendly-graphql-4134)
- [ZeroQL - C# GraphQL client adds fragments support](https://dev.to/byme8/zeroql-c-graphql-client-adds-fragments-support-1lcf)
- [ZeroQL V2 - C# GraphQL client](https://dev.to/byme8/zeroql-v2-c-graphql-client-1o8d)
- [ZeroQL V3 - C# GraphQL client](https://dev.to/byme8/zeroql-v3-c-friendly-graphql-client-4b8n)

# How to setup

The initial setup:
``` bash
# create console app
dotnet new console -o QLClient
# go to project folder 
cd QLClient
# create manifest file to track nuget tools
dotnet new tool-manifest 
# add ZeroQL.CLI nuget tool
dotnet tool install ZeroQL.CLI
# add ZeroQL nuget package
dotnet add package ZeroQL 
# fetch graphql schema from server(creates schema.graphql file)
dotnet zeroql schema pull http://localhost:10000/graphql
# to bootstrap schema.graphql file from graphql schema
dotnet zeroql generate --schema ./schema.graphql --namespace TestServer.Client --client-name TestServerGraphQLClient --output Generated/GraphQL.g.cs
```

It is possible to add next target to csproj to keep generated client in sync with schema.graphql:
``` xml
<Target Name="GenerateQLClient" BeforeTargets="BeforeCompile">
    <Exec Command="dotnet zeroql generate --schema .\schema.graphql --namespace TestServer.Client --client-name TestServerGraphQLClient --output Generated/GraphQL.g.cs" />
</Target>
```
As a result, the graphql client will be generated on every build.

## Config

There is a way to simplify the CLI command. The command `` dotnet zeroql config init `` creates the `` zeroql.json ``. It may look like that:
``` json
{
  "$schema": "https://raw.githubusercontent.com/byme8/ZeroQL/main/schema.verified.json",
  "graphql": "./schema.graphql",
  "namespace": "ZeroQL.Client",
  "clientName": "ZeroQLClient",
  "output": "./Generated/GraphQL.g.cs"
}
```

Then we can use it like that:
``` bash
dotnet zeroql generate -c ./zeroql.json
```

# How to use

Let's suppose that schema.graphql file contains the following:
``` graphql
schema {
  query: Queries
  mutation: Mutation
}

type Queries {
  me: User!
  user(id: Int!): User
}

type Mutation {
  addUser(firstName: String!, lastName: String!): User!
  addUserProfileImage(userId: Int! file: Upload!): Int!
}

type User {
  id: Int!
  firstName: String!
  lastName: String!
  role: Role!
}

type Role {
  id: Int!
  name: String!
}
```

and we want to execute the query like that:
``` graphql
query { me { id firstName lastName } }
```

## GraphQL lambda syntax

Here how we can achieve it with ZeroQL "lambda" syntax:
``` csharp
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:10000/graphql");

var client = new TestServerGraphQLClient(httpClient);

var response = await client.Query(static o => o.Me(o => new { o.Id, o.FirstName, o.LastName }));

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query { me { id firstName lastName } }
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}"); // 1: Jon Smith
```

You can pass arguments if needed:
``` csharp
var variables = new { Id = 1 };
var response = await client.Query(variables, static (i, o) => o.User(i.Id, o => new { o.Id, o.FirstName, o.LastName }));

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query ($id: Int!) { user(id: $id) { id firstName lastName } }
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}"); // 1: Jon Smith
```

You can fetch attached fields:
``` csharp
var variables = new { Id = 1 };
var response = await client.Query(
    variables,
    static (i, o) => o
        .User(i.Id,
            o => new
            {
                o.Id,
                o.FirstName,
                o.LastName,
                Role = o.Role(role => role.Name)
            }));

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query GetUserWithRole($id: Int!) { user(id: $id) { id firstName lastName role { name }  } }
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}, Role: {response.Data.Role}"); // 1: Jon Smith, Role: Admin
```

## GraphQL request syntax

In more complex queries, the "lambda" syntax may look verbose, and extracting requests into a separate entity would be nice. Now it is possible to do it via the "request" syntax. Here is an example:
``` csharp

// define a request
public record GetUserQuery(int Id) : GraphQL<Queries, UserModel?>
{
    public override UserModel? Execute(Queries query) 
        => query.User(Id, o => new UserModel(o.Id, o.FirstName, o.LastName));
}

// execute a request
var response = await client.Execute(new GetUserQuery(variables.FriendId));

Console.WriteLine(response.Query); // query GetUserQuery($id: Int!) { user(id: $id) { id firstName lastName } }
Console.WriteLine(response.Data); // UserModel { Id = 2, FirstName = Ben, LastName = Smith }

```

You need to create a record from the base record `` GraphQL<TOperationType, TResult> ``. Where the `` TOperationType `` is a root query type(`` Query ``, `` Mutation ``) that associated with the `` GraphQLClient<TQuery, TMutataion> `` instance.

# Benchmarks

The complete benchmark source code you can find [here](https://github.com/byme8/ZeroQL/blob/main/src/Benchmarks/ZeroQL.Benchmark/Program.cs).

The short version looks like this:
``` csharp
[Benchmark]
public async Task<string> Raw()
{
    var rawQuery = @"{ ""query"": ""query { me { firstName }}"" }";
    var response = await httpClient.PostAsync("", new StringContent(rawQuery, Encoding.UTF8, "application/json"));
    var responseJson = await response.Content.ReadAsStreamAsync();
    var qlResponse = JsonSerializer.Deserialize<JsonObject>(responseJson, options);

    return qlResponse["data"]["me"]["firstName"].GetValue<string>();
}

[Benchmark]
public async Task<string> StrawberryShake()
{
    var firstname = await strawberryShake.Me.ExecuteAsync();
    return firstname.Data.Me.FirstName;
}

[Benchmark]
public async Task<string> ZeroQL()
{
    var firstname = await zeroQLClient.Query(static q => q.Me(o => o.FirstName));

    return firstname.Data;
}
```

Here results:
``` ini
BenchmarkDotNet=v0.13.2, OS=macOS 13.2.1 (22D68) [Darwin 22.3.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.200
  [Host]     : .NET 7.0.3 (7.0.323.6910), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 7.0.3 (7.0.323.6910), Arm64 RyuJIT AdvSIMD
```
|              Method |     Mean |   Error |  StdDev |   Gen0 | Allocated |
|-------------------- |---------:|--------:|--------:|-------:|----------:|
|                 Raw | 172.2 μs | 1.49 μs | 1.40 μs | 0.7324 |   4.96 KB |
|     StrawberryShake | 175.0 μs | 1.18 μs | 1.05 μs | 1.4648 |   9.32 KB |
|        ZeroQLLambda | 174.2 μs | 1.26 μs | 1.17 μs | 0.7324 |    5.5 KB |
|       ZeroQLRequest | 174.8 μs | 1.68 μs | 1.49 μs | 0.7324 |   5.88 KB |
|  ZeroQLLambdaUpload | 208.5 μs | 2.06 μs | 1.83 μs | 1.4648 |  10.34 KB |
| ZeroQLRequestUpload | 208.9 μs | 3.02 μs | 2.83 μs | 1.7090 |  10.43 KB |

As you can see, the ``Raw`` method is the fastest.
The ``ZeroQL`` method is a bit faster than the ``StrawberryShake`` method. 
But in absolute terms, all of them are pretty much the same.

So, with the `` ZeroQL `` you can forget about the graphql and just use the Linq-like interface. 
It will have little effect on performace.
