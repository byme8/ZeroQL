using System;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using GraphQL.TestServer;
using ZeroQL.Core;
using ZeroQL.TestApp.Models;

namespace ZeroQL.TestApp;

public class Program
{
    public static void Stub()
    {
        UserModel user;
    }

    public static async Task Main()
    {
        await Execute();
    }

    public static async Task<IGraphQLResult> Execute()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:10000/graphql")
        };

        var qlClient = new TestServerClient(httpClient);
        // place to replace
        var response = await qlClient.Query(static q => q.Me(o => o.FirstName));

        return response;
    }
}

public class AddProfileImage
{
    public AddProfileImage(int userId, Upload file)
    {
        UserId = userId;
        File = file;
    }

    public int UserId { get; set; }

    public Upload File { get; set; }
}