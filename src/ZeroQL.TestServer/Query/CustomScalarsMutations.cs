#define UserId

namespace ZeroQL.TestServer.Query;


#if UserId && !UserIdNative
public class UserId
{
    public string Value { get; private set; }

    public static UserId Create(string value) => new UserId() { Value = value };
}
#endif

[ExtendObjectType(typeof(Mutation))]
public class CustomScalarsMutations
{
}