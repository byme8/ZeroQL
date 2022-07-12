## ZeroQL - C# friendly GraphQL client

Nowadays, GraphQL has become a more and more popular technology for building web servers. At the same time, the C# lacks a "native" client for it. Under "native" I mean an ability to build queries inside the C# without the need to write raw GraphQL and be sure that if your project was able to compile, then it will work as expected.

I was looking for such a tool continuously, and the closes what I managed to find was [Strawberry Shake](https://chillicream.com/docs/strawberryshake/). It requires you to write a raw GraphQL. At the same time, it will generate all the necessary wrappings for C#, and you will have a nice type-safe API to work with.

I have been using it a lot, but I wanted to have something even more native to simplify integration between different parts of our app.
My goal is to have a public interface that allows to execute queries like that:
``` csharp
var response = await client.Query(q => q.User(42, user => new { user.Id, user.FirstName, user.LastName });
```
It will be equivalent to following GraphQL query:
``` graphql
query { user(id: 42) { id firstName lastName } }
```
After some investigation, I found a way to achieve what I wanted. Not everything went well, but the core idea is there. 

So, meet the [ZeroQL](https://github.com/byme8/ZeroQL)! It is C# friendly GraphQL client with a Linq-like interface and excellent performance equivalent to a simple HTTP call.

## Action
Let's see it in action with an example. Let's suppose that we have a local [HotChocolate](https://chillicream.com/docs/hotchocolate) server on localhost:10000, and it serves the following GraphQL schema:
``` graphql
schema {
  query: Query
  mutation: Mutation
}

type Query {
  me: User!
  user(id: Int!): User
}

type Mutation {
  addUser(firstName: String!, lastName: String!): User!
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

Now, let's create a console app that can access it. We can do it with the next commands:
``` bash
dotnet new console -o QLClient # create console app
cd QLClient # go to the project folder
curl http://localhost:10000/graphql?sdl > schema.graphql # fetch graphql schema from server
dotnet new tool-manifest # create manifest file to track NuGet tools
dotnet tool install ZeroQL.CLI # add ZeroQL.CLI NuGet tool
dotnet add package ZeroQL # add ZeroQL NuGet package
dotnet zeroql generate --schema .\schema.graphql --namespace TestServer.Client --client-name TestServerGraphQLClient --output Generated/GraphQL.g.cs # generate wrappers from the schema.graphql
```

The last step can be placed in a separate target inside the csproj file to ensure we have the latest changes from schama.graphql. It may look like that:
``` xml
<Target Name="GenerateQLClient" BeforeTargets="BeforeCompile">
    <Exec Command="dotnet zeroql generate --schema .\schema.graphql --namespace TestServer.Client --client-name TestServerGraphQLClient --output Generated/GraphQL.g.cs" />
</Target>
```
It may look a bit complicated at first glance, but don't be afraid we need to do it only once. 

The initial setup is ready, and we can execute our first query. Let's modify the Program.cs to look like that:
``` csharp
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:10000/graphql");

var client = new TestServerGraphQLClient(httpClient);

var response = await client.Query(static q => q.Me(o => new { o.Id, o.FirstName, o.LastName }));

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query { me { id firstName lastName } }
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}"); // 1: Jon Smith
```
As you can see, the workflow is straightforward. Create the GraphQL client, write a query in C#, execute it and get results. Let's look at it in detail. 

The class `` TestServerGraphQLClient `` is generated via ZeroQL.CLI. It has a method ``Query`` that accepts a "graphql" lambda**(not expression)**. This "graphql" lambda takes one argument of type `` Query ``. It generated too. Then the source generator will look inside the lambda, analyzes, and transforms it into the corresponding GraphQL. Then it is placed in a "special" dictionary. This dictionary contains the stringified lambda and the associated graphql. And if you look at the `` Query `` method itself, you will see that it has a hidden argument `` queryKey ``:
``` c# 
public async Task<GraphQLResult<TResult>> Query<TResult>(
    Func<TQuery, TResult> query,
    [CallerArgumentExpression("query")] string queryKey = null!)
{
    return await Execute<Unit, TQuery, TResult>(OperationKind.Query, null, null, (i, q) => query(q), queryKey);
}
```
The `` CallerArgumentExpression `` is a new feature of C# 10. It allows us to get a stringified representation of the expression that was passed inside—that exact representation we need to get the corresponding graphql from the "special" dictionary. As a result, we always know what graphql we need for each call. The crucial thing here is that the graphql is generated at **compile-time**. So, there is nothing to do at runtime except executing the HTTP call. As a result, we have our GraphQL from C# code at compile-time and zero overhead at runtime. 
Another important thing is that the "graphql" lambda must be a static one. There are two reasons for that. First, analyzing it via the source generator is much easier because there are no outside-scope variables that can make things complicated. Second, if you plan to have graphql variables like that: 
``` graphql
var variables = new { Id = 1 };
var response = await client.Query(variables, static (i, q) => q.User(i.Id, o => new { o.Id, o.FirstName, o.LastName }));
```
It is the single way to ensure that all inputs are analyzed. With such an approach, we have them as parameters and can serialize them at runtime before the HTTP call.

We can get deep nested fields too:
``` graphql
var variables = new { Id = 1 };
var response = await client.Query(
    "GetUserWithRole",
    variables,
    static (i, q) => q
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

Also, touch multiple fields at the same time:
``` 
var variables = new { Id = 1 };
var response = await client.Query(
    "GetMeAndUser",
    variables,
    static (i, q) => new
    {
        MyFirstName = q.Me(o => o.FirstName),
        User = q.User(i.Id,
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



## Supported use cases

