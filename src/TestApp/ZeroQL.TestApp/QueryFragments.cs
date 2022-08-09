using System;
using GraphQL.TestServer;
using ZeroQL.Core;
using ZeroQL.TestApp.Models;

namespace ZeroQL.TestApp;

public static partial class QueryFragments
{
    [GraphQLFragment]
    public static partial UserModel ExposedFragmentUserWithRole(this User user)
    {
        return new UserModel(user.FirstName, user.LastName, user.Role(o => o.Name));
    }
    
    [GraphQLFragment]
    public static UserModel AsUserWithRoleNameBody(this User user)
    {
        return new UserModel(user.FirstName, user.LastName, user.Role(o => o.Name));
    }

    [GraphQLFragment]
    public static UserModel AsUserWithRoleNameInitializers(this User user)
    {
        return new UserModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role(o => o.Name)
        };
    }

    [GraphQLFragment]
    public static UserModel AsUserWithRoleNameExpression(this User user)
        => new UserModel(user.FirstName, user.LastName, user.Role(o => o.Name));

    [GraphQLFragment]
    public static UserModel? GetUserById(this Query query, int id)
        => query.User(id, o => o!.AsUserWithRoleNameBody());
}