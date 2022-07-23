## ZeroQL - C# GraphQL client with fragments support

In my previous [article](https://dev.to/byme8/zeroql-c-friendly-graphql-4134) I introduce you to ZeroQL. 
It is a C# "native" graphql client with a Linq-like interface with no compromises on the performance.
 In this article, I want to present support for fragments.

## What do fragments means?

In the graphql, you have to specify every field that you want to fetch from the server like that:
``` graphql
query GetMe {
    me {
        id
        firstName
        lastName
    }
}
```
It is fine if you need to do it only once, but, in some cases, you may want to get the same set of fields again and again. It may become too verbose:
``` graphql
query GetMeAndFriend($friendId: Int!) {
    me {
        id
        firstName
        lastName
    }
    user(id: $friendId) {
        id
        firstName
        lastName
    }
}
```
To make life easier, GraphQL has support fragments. They allow to define a set of fields and then reuse them when needed.
``` graphql
fragment UserFields on User {
    id
    firstName
    lastName
}

query GetMeAndFriend($friendId: Int!) {
    me {
        ...UserFields
    }
    user(id: $friendId) {
        ...UserFields
    }
}
```
I guess the idea is clear. Now let's see how we can create fragments in side the C#.

We will work with next GraphQL schema:
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

> How to setup the ZeroQL you can find in [this article](https://dev.to/byme8/zeroql-c-friendly-graphql-4134).

The `` GetMeAndFriend `` GraphQL query from the sample above looks like that:
``` csharp
var variables = new { FriendId = 2 };
var response = await client.Query(
    variables, 
    static (i, q) => new
    {
        Me = q.Me(o => new { o.Id, o.FirstName, o.LastName }),
        User = q.User(i.FriendId, o => new { o.Id, o.FirstName, o.LastName }),
    });

Console.WriteLine(response.Query); // query ($friendId: Int!) { me { id firstName lastName }  user(id: $friendId) { id firstName lastName } }
Console.WriteLine(response.Data); //  { Me = { Id = 1, FirstName = Jon, LastName = Smith }, User = { Id = 2, FirstName = Ben, LastName = Smith } }
```
Let's move user fields into a fragment. For this, we need a user model:
``` csharp
public record UserModel
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```
and an extension method:
``` csharp
public static class UserFragments
{
    [GraphQLFragment]
    public static UserModel AsUserModel(this User user)
    {
        return new UserModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}
```
Now we can rewrite the query like that:
``` csharp
var variables = new { FriendId = 2 };
var response = await client.Query(
    variables, 
    static (i, q) => new
    {
        Me = q.Me(o => o.AsUserModel()),
        User = q.User(i.FriendId, o => o.AsUserModel()),
    });

Console.WriteLine(response.Query); // query ($friendId: Int!) { me { id firstName lastName }  user(id: $friendId) { id firstName lastName } }
Console.WriteLine(response.Data); // { Me = UserModel { Id = 1, FirstName = Jon, LastName = Smith }, User = UserModel { Id = 2, FirstName = Ben, LastName = Smith } }
```
And everything works as expected. If you look at the generated GraphQL query, you will see that, strictly speaking, it is not a "graphql fragment." It is more of a sub-query that is inserted into a final query. So, we can go further and combine multiple queries like that:
``` csharp
var variables = new { FriendId = 2 };
var response = await client.Query(
    variables, 
    static (i, q) => q.GetMeAndFriend(i.FriendId));

Console.WriteLine(response.Query); // query ($friendId: Int!) { me { id firstName lastName }  user(id: $friendId) { id firstName lastName } }
Console.WriteLine(response.Data); // MeAndFriendResponse { Me = UserModel { Id = 1, FirstName = Jon, LastName = Smith }, Friend = UserModel { Id = 2, FirstName = Ben, LastName = Smith } }

// ...

public record MeAndFriendResponse
{
    public UserModel Me { get; set; }
    public UserModel Friend { get; set; }
}

public static class QueryFragments
{
    [GraphQLFragment]
    public static MeAndFriendResponse GetMeAndFriend(this Query query, int friendId)
    {
        return new MeAndFriendResponse
        {
            Me = query.Me(o => o.AsUserModel()),
            Friend = query.User(friendId, o => o.AsUserModel())
        };
    }
}
```
And again, everything works as expected.

## Limitations

There is one thing that can make life a bit complicated. The source generator looks through the source code to generate the query. It also needs to look into every sub-query, which is fine until it is defined in another assembly/project. In this case, the source generator can't dig into it because the source code for such assemblies is not available. As a result, we can't generate a query for them. If we want to make the fragment work, it should be defined in the same assembly that we call the `` client.Query `` or `` client.Mutation `` methods. This requirement definitely can break some workflows and expectations. At the same time, how often do you need to split your SQL queries into different assemblies? This happens, but not so often. In the worst-case scenario, you must copy-paste fragments from one project to another. 

Again, it doesn't mean this issue can not be fixed in the future. I have some ideas on how to solve it, but at the moment, it is just ideas with tricky implementation. Meanwhile fight is not over.

## Performance

I have said that the ZeroQL has excellent performance. But how excellent is it? 
The repository contains a [benchmark](https://github.com/byme8/ZeroQL/blob/main/src/Benchmarks/ZeroQL.Benchmark/Program.cs). It compares raw graphql request, StrawberryShake, and ZeroQL.

The short version looks like that:
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
    var firstname = await strawberryShake.Me.ExecuteAsync(); // query { me { firstName }}
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
The ``ZeroQL`` method is a bit faster than the ``StrawberryShake`` method. 
But in absolute terms, all of them are pretty much the same. The difference is negligible.

## Summary

So, with the `` ZeroQL `` you can forget about the graphql and just use a fully typed Linq-like interface. It will have little effect on performance. In the future, I plan to figure out how to generate queries for fragments defined in another assembly and think about how to make the initial setup more straightforward.

Thank you for your time! 
If you like what I am working on. Please, give a start to the [Github](https://github.com/byme8/ZeroQL) repository so more people can see it.

## Links

[Github](https://github.com/byme8/ZeroQL)
[NuGet](https://www.nuget.org/packages/ZeroQL)