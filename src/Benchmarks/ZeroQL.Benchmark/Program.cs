using BenchmarkDotNet.Running;
using GraphQL.TestServer;
using ZeroQL;
using ZeroQL.Benchmark;

var serverContext = new ZeroQL.TestServer.Program.ServerContext();

_ = ZeroQL.TestServer.Program.StartServer(serverContext);
await ZeroQL.TestServer.Program.VerifyServiceIsRunning(serverContext);

var benchmark = new RawVsZeroQLBenchmark();
var raw = await benchmark.Raw();
var strawberry = await benchmark.StrawberryShake();
var zeroQLLambda = await benchmark.ZeroQLLambda();
var zeroQLRequest = await benchmark.ZeroQLRequest();

if (!(raw == strawberry && strawberry == zeroQLLambda && zeroQLLambda == zeroQLRequest))
{
    Console.WriteLine("Raw, StrawberryShake and ZeroQL are not equal");
    return;
}

if (!File.Exists(GenerationBenchmark.SchemaFile))
{
    var path = new Uri(GenerationBenchmark.SchemaFile).AbsolutePath;
    Console.WriteLine($"Unable to find schema file: {path}");
    return;
}

var switcher = new BenchmarkSwitcher(new[]
{
    typeof(OldVariablesVsNewVariablesBenchmark),
    typeof(RawVsZeroQLBenchmark),
    typeof(GenerationBenchmark),
});

switcher.Run(args);


ZeroQL.TestServer.Program.StopServer(serverContext);

public record GetMeQuery : GraphQL<Query, string>
{
    public override string Execute(Query query)
        => query.Me(o => o.FirstName);
}

public record AddAvatar(int Id, Upload File) : GraphQL<Mutation, int>
{
    public override int Execute(Mutation mutation)
        => mutation.AddUserProfileImage(Id, File);
}
