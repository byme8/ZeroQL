using GraphQL.TestServer;
using LinqQL.Core;

var httpClient = new HttpClient
{
    BaseAddress = new("http://localhost:10000/")
};

var qlClient = new GraphQLClient<Query>(httpClient);
var result = qlClient
    .Query(
        new { FirstId = 10, SecondId = 20 },
        static (a, q) => new
        {
            User = q.User(a.FirstId, o => new User(o.FirstName, o.LastName, o.Role(o => o.Name))),
            SecondUser = q.User(a.SecondId, o => new
            {
                o.FirstName
            })
        });

var firstUser = result.User;
var secondUser = result.SecondUser;


public record User(string FirstName, string LastName, string Role);