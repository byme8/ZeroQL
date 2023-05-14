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
var zeroQLLambdaOld = await benchmark.ZeroQLLambdaWithoutClosure();
var zeroQLLambdaNew = await benchmark.ZeroQLLambdaWithoutClosure();
var zeroQLRequest = await benchmark.ZeroQLRequest();

if (!(raw == strawberry && strawberry == zeroQLLambdaOld && zeroQLLambdaOld == zeroQLRequest && zeroQLRequest == zeroQLLambdaNew))
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

public record GetUserQuery(int id) : GraphQL<Query, User?>
{
    public override User? Execute(Query query)
        => query.User(id, o => new User(o.Id, o.FirstName, o.LastName));
}

public record User(string Id, string FirstName, string LastName);

public record AddAvatar(int Id, Upload File) : GraphQL<Mutation, int>
{
    public override int Execute(Mutation mutation)
        => mutation.AddUserProfileImage(Id, File);
}
