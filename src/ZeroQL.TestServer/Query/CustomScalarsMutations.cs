#define UserId

using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer.Query;

[MutationType]
public class CustomScalarsMutations
{
    public Uuid CreateUserId(Uuid id)
    {
        return id;
    }
}