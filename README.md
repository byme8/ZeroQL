# ZeroQL | ![GitHub](https://img.shields.io/github/license/byme8/ZeroQL?color=blue&style=flat-square) ![Nuget](https://img.shields.io/nuget/v/zeroql?color=blue&style=flat-square)

The ZeroQL is a high-performance C#-friendly GraphQL client. It supports Linq-like syntax and provides performance close to a simple HTTP call.

# How to use

Library setup may look a bit complicated a first, but you need to do only once.
Then you will be able to use the library without any issues.

``` bash
dotnet new console -o QLClient # create console app
cd QLClient # go to project folder
curl http://localhost:10000/graphql?sdl > schema.graphql # fetch graphql schema from server(depends on your web server)
dotnet new tool-manifest # create manifest file to track nuget tools
dotnet tool install ZeroQL.CLI # add ZeroQL.CLI nuget tool
dotnet add package ZeroQL # add ZeroQL nuget package
```

And the last thing. Add the next target to your project.

``` xml
 <Target Name="GenerateQLClient" BeforeTargets="BeforeCompile">
    <Exec Command="dotnet zeroql generate --schema .\schema.graphql --namespace TestServer.Client --client-name TestServerGraphQLClient --output Generated/GraphQL.g.cs" />
</Target>
```

The graphql client will be generated on every build.
It allows us to keep track of what we have in the schema.graphql file.

Let's suppose that schema.graphql file contains the following:
``` graphql
schema {
  query: Query
}

type Query {
  me: User!
}

type User {
  id: Int!
  firstName: String!
  lastName: String!
}
```

and we want to execute the following query:
``` graphql
query { me { id firstName lastName } }
```

We can do it like that:
``` csharp
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:10000/graphql");

var client = new TestServerGraphQLClient(httpClient);

var response = await client.Query(static o => o.Me(o => new { o.Id, o.FirstName, o.LastName }));

Console.WriteLine($"GraphQL: {response.Query}");
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}");
```

The console output will be:
```
GraphQL: query { me { id firstName lastName } }
-1: Jon Smith
```

