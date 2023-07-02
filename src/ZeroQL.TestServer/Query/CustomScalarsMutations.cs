#define UserId

using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(Mutation))]
public class CustomScalarsMutations
{
    public Uuid CreateUserId(Uuid id)
    {
        return id;
    }
}