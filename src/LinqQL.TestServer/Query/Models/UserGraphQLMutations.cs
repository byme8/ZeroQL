namespace LinqQL.TestServer.Query.Models;

public class Mutation
{
}

[ExtendObjectType(typeof(Mutation))]
public class UserGraphQLMutations
{
    public User AddUser(string firstName, string lastName)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName
        };

        return user;
    }
}