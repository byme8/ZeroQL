namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(Query))]
public class LongOperationsExtensions
{
    public async Task<int> GetLongOperation()
    {
        await Task.Delay(10_000);
        return 42;
    }
}