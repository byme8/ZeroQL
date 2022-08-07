using GraphQL.TestServer;
using ZeroQL.Core;

namespace ZeroQL.TestApp.Models;

public static class BrokenQueryFragments
{
    [GraphQLFragment]
    [GraphQLQueryTemplate("firstName lastName role { name }")]
    public static UserModal AsUserFromDifferentAssembly(this User user)
    {
        return new UserModal(user.FirstName, user.LastName, user.Role(o => o.Name));
    }

    [GraphQLFragment]
    [GraphQLQueryTemplate("user(id: {{userId}}) { firstName lastName role { name } }")]
    public static UserModal AsUserFromDifferentAssembly(this Query query, int userId)
    {
        return query.User(userId, o => o.AsUserFromDifferentAssembly());
    }
}