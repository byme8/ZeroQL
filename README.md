# ZeroQL | [![GitHub](https://img.shields.io/github/license/byme8/ZeroQL?color=blue&style=flat-square)](https://github.com/byme8/ZeroQL/blob/main/LICENCE) [![Nuget](https://img.shields.io/nuget/v/zeroql?color=blue&style=flat-square)](https://www.nuget.org/packages/ZeroQL) [![.NET](https://github.com/byme8/ZeroQL/actions/workflows/dotnet.yml/badge.svg)](https://github.com/byme8/ZeroQL/actions/workflows/dotnet.yml)

The ZeroQL is a high-performance C#-friendly GraphQL client. It supports Linq-like syntax. It does't not require Reflection.Emit or expressions. As a result, in runtime provides performance very close to a raw HTTP call.

There is a more detailed list of what the ZeroQL can do at the moment:
- [ ] Bootstrap schema.graphql file from graphql endpoint
- [x] Bootstrap client from schema.graphql file
- [x] Support for queries
- [x] Support for mutations
- [x] Support for fragments
- [ ] Support for subscriptions
- [ ] Support for @defer
- [ ] Support for @stream

The articles:
- [ZeroQL - C# friendly graphql client](https://dev.to/byme8/zeroql-c-friendly-graphql-4134)

# How to setup

The initial setup:
``` bash
# create console app
dotnet new console -o QLClient
# go to project folder 
cd QLClient
# fetch graphql schema from server(depends on your web server)
curl http://localhost:10000/graphql?sdl > schema.graphql 
# create manifest file to track nuget tools
dotnet new tool-manifest 
# add ZeroQL.CLI nuget tool
dotnet tool install ZeroQL.CLI
# add ZeroQL nuget package
dotnet add package ZeroQL 
# to bootstrap schema.graphql file from graphql schema
dotnet zeroql generate --schema .\schema.graphql --namespace TestServer.Client --client-name TestServerGraphQLClient --output Generated/GraphQL.g.cs
```

It is possible to add next target to csproj to keep generated client in sync with schema.graphql:
``` xml
<Target Name="GenerateQLClient" BeforeTargets="BeforeCompile">
    <Exec Command="dotnet zeroql generate --schema .\schema.graphql --namespace TestServer.Client --client-name TestServerGraphQLClient --output Generated/GraphQL.g.cs" />
</Target>
```
As a result, the graphql client will be generated on every build.

# How to use

Let's suppose that schema.graphql file contains the following:
``` graphql
schema {
  query: Query
}

type Query {
  me: User!
  user(id: Int!): User
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

Here how we can achieve it with ZeroQL:
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

# Fragments
It is possible to define and reuse fragments:
``` csharp
public static class UserFragments
{
    [GraphQLFragment]
    public static UserDetails ToUserDetails(this User user)
    {
        return new UserDetails
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}

var variables = new { Id = 1 };
var response = await client.Query(
    variables,
    static (i, q) => 
        new 
        { 
            Me = q.Me(o => o.ToUserDetails()),
            User = q.User(i.Id, o => o.ToUserDetails())
        });


Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query ($id: Int!) { me { id firstName lastName } user(id: $id) { id firstName lastName } }
Console.WriteLine($"{response.Data.Me.Id}: {response.Data.Me.FirstName} {response.Data.Me.LastName}"); // 1: Jon Smith
Console.WriteLine($"{response.Data.User.Id}: {response.Data.User.FirstName} {response.Data.User.LastName}"); // 1: Jon Smith
```

The fragment has a bunch of limitations.
It should be marked wit the `` [GraphQLFragment] `` attribute.
It should be an extension method.
And it has to be defined in the same assembly where the method ``Query`` is called.
The last limitation is dictated by the fact that source generators can analyze only one assembly once.
So, if you define a fragment in a different assembly, it will not be available for the source generator.


# Benchmarks

The full benchmark source code you can find [here](https://github.com/byme8/ZeroQL/blob/main/src/Benchmarks/ZeroQL.Benchmark/Program.cs)
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

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.4 (21F79) [Darwin 21.5.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.302
  [Host]     : .NET 6.0.7 (6.0.722.32202), Arm64 RyuJIT
  DefaultJob : .NET 6.0.7 (6.0.722.32202), Arm64 RyuJIT


```
|          Method |     Mean |   Error |  StdDev |  Gen 0 | Allocated |
|---------------- |---------:|--------:|--------:|-------:|----------:|
|             Raw | 182.5 μs | 1.07 μs | 1.00 μs | 2.4414 |      5 KB |
| StrawberryShake | 190.9 μs | 0.74 μs | 0.69 μs | 3.1738 |      6 KB |
|          ZeroQL | 185.9 μs | 1.39 μs | 1.30 μs | 2.9297 |      6 KB |

As you can see, the ``Raw`` method is the fastest.
The ``ZeroQL`` method is a bit faster then the ``StrawberryShake`` method. 
But in absolute terms, all of them are pretty much the same.
At the same time, with the `` ZeroQL `` you can forget about the graphql and just use Linq-like interface.
