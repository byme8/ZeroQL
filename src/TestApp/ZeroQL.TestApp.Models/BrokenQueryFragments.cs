using GraphQL.TestServer;
using ZeroQL.Core;

namespace ZeroQL.TestApp.Models;

public static class BrokenQueryFragments
{
    [GraphQLFragment]
    public static UserModal AsUserFromDifferentAssembly(this User user)
    {
        return new UserModal(user.FirstName, user.LastName, user.Role(o => o.Name));
    }
}