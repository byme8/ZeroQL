using BuildZeroQL.Client;
using BuildZeroQL2.Client;

var httpClient = new HttpClient();
var client = new BuildZeroQLClient(httpClient);
var client2 = new BuildZeroQLClient2(httpClient);

Console.WriteLine(await client.Query(q => q.Me(o => o.FirstName)));
Console.WriteLine(await client2.Query(q => q.Me(o => o.FirstName)));
