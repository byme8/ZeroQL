using System;
using System.Threading.Tasks;
using GraphQL.TestServer;

namespace TestStandardLibrary
{
    public class Client
    {
        public async Task Execute()
        {
            var httpClient = new System.Net.Http.HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:10000/graphql");
            
            var graphQLClient = new TestServerClient(httpClient);
            var response = await graphQLClient.Query(o => o.Me(o => o.FirstName));
        }
    }
}