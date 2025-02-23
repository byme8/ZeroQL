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
- [x] [Support for scalars](https://github.com/byme8/ZeroQL/wiki/User-scalars)
- [x] [Support for file uploads](https://github.com/byme8/ZeroQL/wiki/File-upload)
- [x] [Support for persisted queries](https://github.com/byme8/ZeroQL/wiki/Persisted-queries)
- [ ] Support for @defer
- [ ] Support for @stream

You can find the full wiki [here](https://github.com/byme8/ZeroQL/wiki) or just by clicking on the feature bullet point you are interested in.

Check out our articles to learn more about ZeroQL:
- [ZeroQL - C# friendly graphql client](https://dev.to/byme8/zeroql-c-friendly-graphql-4134)
- [ZeroQL - C# GraphQL client adds fragments support](https://dev.to/byme8/zeroql-c-graphql-client-adds-fragments-support-1lcf)
- [ZeroQL V2](https://dev.to/byme8/zeroql-v2-c-graphql-client-1o8d)
- [ZeroQL V3](https://dev.to/byme8/zeroql-v3-c-friendly-graphql-client-4b8n)
- [ZeroQL V6](https://dev.to/byme8/zeroql-linq-graphql-client-updates-2akn)

# How to setup

> Here, you can find the setup for net6.0+ projects. 
> You can find [netstandard or .Net Framework](https://github.com/byme8/ZeroQL/wiki/netstandard-setup) and [Unity](https://github.com/byme8/ZeroQL/wiki/Unity-setup) setup in [wiki](https://github.com/byme8/ZeroQL/wiki).

The initial setup:
``` bash
# create console app
dotnet new console -o QLClient
# go to project folder 
cd QLClient
# create manifest file to track nuget tools
dotnet new tool-manifest 
# add ZeroQL.CLI nuget tool
dotnet tool install ZeroQL.CLI # or 'dotnet tool restore' once you pulled the existing repository
# add ZeroQL nuget package
dotnet add package ZeroQL 
# fetch graphql schema from server(creates schema.graphql file)
dotnet zeroql schema pull --url http://localhost:10000/graphql
# to create ZeroQL config file: ./config.zeroql.json
dotnet zeroql config init
# build the project to initiate the ZeroQL client generation with options specified inside config.zeroql.json
dotnet build
```

The build should be successful, and now we can use the generated client.

## Config

The command `` dotnet zeroql config init `` creates the `` config.zeroql.json ``. By itself it looks like that:
``` json
{
  "$schema": "https://raw.githubusercontent.com/byme8/ZeroQL/main/schema.verified.json",
  "graphql": "./schema.graphql",
  "namespace": "ZeroQL.Client",
  "clientName": "ZeroQLClient"
}
```
Now if you have `` ZeroQL `` package installed to your `` csproj ``, it will automatically detect and execute CLI based on this configuration file on every build. To make sure that it works, the config file should follow the `` *.zeroql.json ``pattern, or you can add a custom definition in your `` csproj `` like that:
``` xml
<ItemGroup>
    <ZeroQLConfig Include="you.custom.config.name.json"/>
</ItemGroup>
```
The generated client would be stored inside ``./obj/ZeroQL `` folder. So it will never appear in the solution. However, you still have access to generated classes in your source code.

If you want to turn off automatic generation on every build, it is possible to disable it:
``` xml
<PropertyGroup>
   <ZeroQLOnBuildTriggerEnabled>False</ZeroQLOnBuildTriggerEnabled>
</PropertyGroup>
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

Here is how we can achieve it with ZeroQL "lambda" syntax:
``` csharp
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:10000/graphql");

var client = new TestServerGraphQLClient(httpClient);

var response = await client.Query(o => o.Me(o => new { o.Id, o.FirstName, o.LastName }));

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query { me { id firstName lastName } }
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}"); // 1: Jon Smith
```

You can pass arguments inside lambda if needed:
``` csharp
var userId = 1;
var response = await client.Query(o => o.User(userId, o => new User(o.Id, o.FirstName, o.LastName)));

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query ($id: Int!) { user(id: $id) { id firstName lastName } }
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}"); // 1: Jon Smith
```

There is a limitation for lambda syntax. The variable should be a local variable or a parameter of the function.
Otherwise, it will not be included in the lambda closure. As a result, ZeroQL would not be able to get a value.

Here is an example of the function parameter:
``` csharp
public Task<User> GetUser(int userId)
{
    var response = await client.Query(o => o.User(userId, o => new User(o.Id, o.FirstName, o.LastName)));
    return response.Data;
}
```
To be clear, you don't need actively account for it. ZeroQL will analyze and report errors if something is wrong.

For example, the next sample will not work:
``` csharp

public int UserId { get; set; }

public Task<User> GetUser()
{
    var response = await client.Query(o => o.User(UserId, o => new User(o.Id, o.FirstName, o.LastName))); // ZeroQL will report a compilation error here
    return response.Data;
}
```

Also, there is a way to avoid lambda closure:
``` csharp
var variables = new { Id = 1 };
var response = await client.Query(variables, static (i, o) => o.User(i.Id, o => new User(o.Id, o.FirstName, o.LastName)));
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

You need to create a record from the base record `` GraphQL<TOperationType, TResult> ``. Where the `` TOperationType `` is a root query type(`` Query ``, `` Mutation ``) that is associated with the `` GraphQLClient<TQuery, TMutataion> `` instance.

# Benchmarks

The complete benchmark source code you can find [here](https://github.com/byme8/ZeroQL/blob/main/src/Benchmarks/ZeroQL.Benchmark/RawVsZeroQLBenchmark.cs).

The short version looks like this:
``` csharp
[Benchmark]
public async Task<string> Raw()
{
    var rawQuery = 
        $$"""
        {
            "variables": { "id": {{id}} }, 
            "query": "query GetUser($id: Int!){ user(id: $id) { id firstName lastName } }" 
        }
        """;
    var response = await httpClient.PostAsync("", new StringContent(rawQuery, Encoding.UTF8, "application/json"));
    var responseJson = await response.Content.ReadAsStreamAsync();
    var qlResponse = JsonSerializer.Deserialize<JsonObject>(responseJson, options);

    return qlResponse!["data"]!["user"]!["firstName"]!.GetValue<string>();
}

[Benchmark]
public async Task<string> StrawberryShake()
{
    // query GetUser($id: Int!) {
    //   user(id: $id) {
    //       id
    //       firstName
    //       lastName
    //   }
    // }
    var firstname = await strawberryShake.GetUser.ExecuteAsync(id);
    return firstname.Data!.User!.FirstName;
}

[Benchmark]
public async Task<string> ZeroQLLambdaWithoutClosure()
{
    var variables = new { Id = id };
    var firstname = await zeroQLClient.Query(
        variables, static (i, q)
            => q.User(i.Id, o => new { o.Id, o.FirstName, o.LastName }));

    return firstname.Data!.FirstName;
}

[Benchmark]
public async Task<string> ZeroQLLambdaWithClosure()
{
    var id  = this.id;
    var firstname = await zeroQLClient.Query( q
            => q.User(id, o => new { o.Id, o.FirstName, o.LastName }));

    return firstname.Data!.FirstName;
}

[Benchmark]
public async Task<string> ZeroQLRequest()
{
    var firstname = await zeroQLClient.Execute(new GetUserQuery(id));

    return firstname.Data!.FirstName;
}

// ..
public record GetUserQuery(int id) : GraphQL<Query, User?>
{
    public override User? Execute(Query query)
        => query.User(id, o => new User(o.Id, o.FirstName, o.LastName));
}
```

Here results:
``` ini

BenchmarkDotNet=v0.13.2, OS=macOS 15.0.1 (24A348) [Darwin 24.0.0]
Apple M3 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK=9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD


```
|                     Method |     Mean |    Error |   StdDev |   Gen0 | Allocated |
|--------------------------- |---------:|---------:|---------:|-------:|----------:|
|                        Raw | 64.68 μs | 0.409 μs | 0.383 μs | 0.4883 |   5.32 KB |
|            StrawberryShake | 69.06 μs | 0.371 μs | 0.347 μs | 1.3428 |  11.79 KB |
| ZeroQLLambdaWithoutClosure | 66.61 μs | 0.457 μs | 0.428 μs | 0.8545 |   7.07 KB |
|    ZeroQLLambdaWithClosure | 66.90 μs | 0.419 μs | 0.392 μs | 0.8545 |   7.55 KB |
|              ZeroQLRequest | 66.47 μs | 0.488 μs | 0.457 μs | 0.7324 |   6.65 KB |

As you can see, the ``Raw`` method is the fastest.
The ``ZeroQL`` method is a bit faster than the ``StrawberryShake`` method. 
But in absolute terms, all of them are pretty much the same.

So, with the `` ZeroQL ``, you can forget about the graphql and just use the Linq-like interface. 
It will have little effect on performance.

# Credits

The initial inspiration for this project came from the work done at https://github.com/Giorgi/GraphQLinq
