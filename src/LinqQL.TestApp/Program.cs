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
            var httpClient = new HttpClient
            {
                BaseAddress = new("http://localhost:10000/")
            };

            var qlClient = new GraphQLClient<Query>(httpClient);
            // place for query
        }
    }

    public record User(string FirstName, string LastName, string Role);
}