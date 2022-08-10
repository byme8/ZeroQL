using GraphQL.TestServer;
using ZeroQL.Core;
using ZeroQL.TestApp.Models;

public static partial class QueryFragmentsWithoutNamespace
{
    [GraphQLFragment]
    public static partial UserModel AsUserWithoutNamespace(this User user)
    {
        return new UserModel(user.FirstName, user.LastName, user.Role(o => o.Name));
    }
}