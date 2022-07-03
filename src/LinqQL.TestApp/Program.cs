using System;
using System.Net.Http;
using System.Threading.Tasks;
using GraphQL.TestServer;
using LinqQL.Core;

namespace LinqQL.TestApp;

public class Program
{
    public static void Stub()
    {

    }

    public static async Task Main()
    {
        await Execute();
    }

    public static async Task<object> Execute()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:10000/graphql")
        };

        var qlClient = new GraphQLClient<TestServerQuery>(httpClient);
        var response = await qlClient.Query(static q => q.Me(o => o.FirstName));

        return response;
    }
}

public record User(string FirstName, string LastName, string Role);