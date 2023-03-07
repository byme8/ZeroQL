using GraphQL.TestServer;
using ZeroQL;
using ZeroQL.TestApp;

// ReSharper disable once CheckNamespace
public static partial class QueryFragmentsWithoutNamespace
{
    [GraphQLFragment]
    public static partial UserModel AsUserWithoutNamespace(this User user)
    {
        return new UserModel(user.FirstName, user.LastName, user.Role(o => o.Name));
    }
}