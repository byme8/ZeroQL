using HotChocolate.Types;

namespace LinqQL.TestServer;

public class Query
{

}

[ExtendObjectType(typeof(Query))]
public class UserGraphQLExtensions
{
    public User[] GetUsers(int page, int size)
    {
        return Enumerable.Range(0, size)
            .Select(o => new User
            {
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString()
            })
            .ToArray();
    }

    public User GetUser(int id)
    {
        return new User
        {
            FirstName = "Jon",
            LastName = "Smith"
        };
    }

    public User? GetAdmin(int id)
    {
        return null;
    }
}

public class User
{
    public string FirstName { get; set; }

    public string LastName { get; set; }
}