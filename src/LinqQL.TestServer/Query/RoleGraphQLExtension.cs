using LinqQL.TestServer.Query.Models;

namespace LinqQL.TestServer.Query;

[ExtendObjectType(typeof(User))]
public class RoleGraphQLExtension
{
    public Role GetRole([Parent] User user)
    {
        return new Role
        {
            Id = 42,
            Name = "Admin"
        };
    }
}