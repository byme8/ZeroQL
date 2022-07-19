# ZeroQL | ![GitHub](https://img.shields.io/github/license/byme8/ZeroQL?color=blue&style=flat-square) ![Nuget](https://img.shields.io/nuget/v/zeroql?color=blue&style=flat-square) [![.NET](https://github.com/byme8/ZeroQL/actions/workflows/dotnet.yml/badge.svg)](https://github.com/byme8/ZeroQL/actions/workflows/dotnet.yml)

The ZeroQL is a high-performance C#-friendly GraphQL client. It supports Linq-like syntax. It does't not require Reflection.Emit or expressions. As a result, in runtime provides performance very close to a raw HTTP call.

There is a more detailed list of what the ZeroQL can do at the moment:
- [ ] Bootstrap schema.graphql file from graphql endpoint
- [x] Bootstrap client from schema.graphql file
- [x] Support for queries
- [x] Support for mutations
- [ ] Support for fragments(in progress)
- [ ] Support for subscriptions
- [ ] Support for @defer
- [ ] Support for @stream

The full article you can find [here](https://dev.to/byme8/zeroql-c-friendly-graphql-4134)

# How to setup

The initial setup:
``` bash
dotnet new console -o QLClient # create console app
cd QLClient # go to project folder
curl http://localhost:10000/graphql?sdl > schema.graphql # fetch graphql schema from server(depends on your web server)
dotnet new tool-manifest # create manifest file to track nuget tools
dotnet tool install ZeroQL.CLI # add ZeroQL.CLI nuget tool
dotnet add package ZeroQL # add ZeroQL nuget package
```

Add the next target to the QLClient.csproj.
``` xml
<Target Name="GenerateQLClient" BeforeTargets="BeforeCompile">
    <Exec Command="dotnet zeroql generate --schema .\schema.graphql --namespace TestServer.Client --client-name TestServerGraphQLClient --output Generated/GraphQL.g.cs" />
</Target>
```

The graphql client will be generated on every build.
It allows to keep track of what is defined in the schema.graphql file.

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

Here how we can achive it with ZeroQL:
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

You can give name to the query:
``` csharp
var variables = new { Id = 1 };
var response = await client.Query("GetUser", variables, static (i, o) => o.User(i.Id, o => new { o.Id, o.FirstName, o.LastName }));

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query GetUser($id: Int!) { user(id: $id) { id firstName lastName } }
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}"); // 1: Jon Smith
```

You can fetch attached fields:
``` csharp
var variables = new { Id = 1 };
var response = await client.Query(
    "GetUserWithRole",
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

You can do multiple queries at the same time:
``` csharp
var variables = new { Id = 1 };
var response = await client.Query(
    "GetMeAndUser",
    variables,
    static (i, o) => new
    {
        MyFirstName = o.Me(o => o.FirstName),
        User = o.User(i.Id,
            o => new
            {
                o.FirstName,
                o.LastName,
                Role = o.Role(role => role.Name)
            })
    });

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query GetUserWithRole($id: Int!) { me { firstName }  user(id: $id) { firstName lastName role { name }  } }
Console.WriteLine($"Me: {response.Data.MyFirstName}, User: {response.Data.User.FirstName} {response.Data.User.LastName}, Role: {response.Data.User.Role}"); // Me: Jon, User: Jon Smith, Role: Admin
```
