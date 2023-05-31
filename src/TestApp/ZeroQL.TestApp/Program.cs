using System;
using System.Net.Http;
using System.IO; // do not remove this
using System.Collections.Generic;
using System.Threading; // do not remove this
using System.Threading.Tasks;
using GraphQL.TestServer;
using ZeroQL;
using ZeroQL.Json; // do not remove this
using ZeroQL.Pipelines; // do not remove this
using ZeroQL.TestApp.Models;

namespace ZeroQL.TestApp;

public class Program
{
    public static void Stub()
    {
#pragma warning disable CS0168
        UserModel user;
#pragma warning restore CS0168
    }

    public static async Task Main()
    {
        await Execute();
        Console.WriteLine("Completed. Press any key to exit...");
        Console.ReadKey();
    }

    // class Program place to replace in 

    public static async Task<IGraphQLResult> Execute(CancellationToken cancellationToken = default)
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