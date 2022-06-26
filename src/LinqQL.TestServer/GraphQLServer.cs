using HotChocolate.Types;

namespace LinqQL.TestServer;

public class Query
{

}

[ExtendObjectType(typeof(Query))]
public class UserGraphQLExtensions
{
    public User GetUser(int id)
    {
        return new User
        {
            FirstName = "Jon",
            LastName = "Smith"
        };
    }
}

public class User
{
    public string FirstName { get; set; }

    public string LastName { get; set; }
}