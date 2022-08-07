using System;
using GraphQL.TestServer;
using ZeroQL.Core;
using ZeroQL.TestApp.Models;

namespace ZeroQL.TestApp;

// public static partial class QueryFragments
// {
//     [GraphQLFragmentQuery("", new string[0] {  })]
//     public static partial UserModal AsUserWithRoleNameBody(this User user);
// }

public static partial class QueryFragments
{
    [GraphQLFragment]
    public static UserModal AsUserWithRoleNameBody(this User user)
    {
        return new UserModal(user.FirstName, user.LastName, user.Role(o => o.Name));
    }

    [GraphQLFragment]
    public static UserModal AsUserWithRoleNameInitializers(this User user)
    {
        return new UserModal
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role(o => o.Name)
        };
    }

    [GraphQLFragment]
    public static UserModal AsUserWithRoleNameExpression(this User user)
        => new UserModal(user.FirstName, user.LastName, user.Role(o => o.Name));

    [GraphQLFragment]
    public static UserModal? GetUserById(this Query query, int id)
        => query.User(id, o => o!.AsUserWithRoleNameBody());
}