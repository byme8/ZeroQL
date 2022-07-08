using LinqQL.TestServer.Query.Models;

namespace LinqQL.TestServer.Query;

[ExtendObjectType(typeof(Query))]
public class UserGraphQLExtensions
{
    public User Me()
    {
        return new User
        {
            Id = -1,
            FirstName = "Jon",
            LastName = "Smith"
        };
    }

    public User[] GetUsers(UserFilterInput filter, int page, int size)
    {
        return Enumerable.Range(0, size)
            .Select(o => new User
            {
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString()
            })
            .ToArray();
    }

    public UserKind[] GetUserKinds()
    {
        return new[]
        {
            UserKind.Bad,
            UserKind.Good
        };
    }

    public User[][] GetUsersMatrix()
    {
        return new[]
        {
            Enumerable.Range(0, 10)
                .Select(o => new User
                {
                    FirstName = Guid.NewGuid().ToString(),
                    LastName = Guid.NewGuid().ToString()
                })
                .ToArray()
        };
    }

    public User[] GetUsersByKind(UserKind kind, int page, int size)
    {
        return Enumerable.Range(0, size)
            .Select(o => new User
            {
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString()
            })
            .ToArray();
    }

    public int[] GetUsersIds(UserKind kind, int page, int size)
    {
        return Enumerable.Range(0, size).ToArray();
    }

    public User GetUser(int id)
    {
        return new User
        {
            Id = id,
            FirstName = "Jon",
            LastName = "Smith"
        };
    }
    
    public UserKind GetUserKind(int id)
    {
        return UserKind.Good;
    }

    public User? GetAdmin(int id)
    {
        return null;
    }

    public TypesContainer GetContainer()
    {
        return new TypesContainer();
    }
}