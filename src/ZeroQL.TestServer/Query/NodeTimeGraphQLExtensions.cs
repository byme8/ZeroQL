using NodaTime;

namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(Query))]
public class NodeTimeGraphQLExtensions
{
    public Instant GetInstant()
    {
        return Instant.FromDateTimeOffset(DateTimeOffset.Now);
    }
}