using GraphQL.TestServer;
using LinqQL.Core;

namespace LinqQL.TestApp
{
    public class Program
    {
        public static void Stub()
        {
            
        }
        
        public static void Main()
        {

        }

        public static async Task<object> Execute()
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new("http://localhost:10000/graphql")
            };

            var qlClient = new GraphQLClient<Query>(httpClient);
            var response = await qlClient.Query(static q => q.Me(o => o.FirstName));

            return response;
        }
    }

    public record User(string FirstName, string LastName, string Role);
}