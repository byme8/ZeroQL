using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(Query))]
public class UserGraphQLExtensions
{
    public static Dictionary<int, User> Users = new()
    {
        { 1, new User() { Id = 1, FirstName = "Jon", LastName = "Smith", UserKind = UserKind.Good } },
        { 2, new User() { Id = 2, FirstName = "Ben", LastName = "Smith", UserKind = UserKind.Bad } },
    };

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
        return Users.Select(o => o.Value.UserKind).ToArray();
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

    public User? GetUser(int id)
    {
        return Users.GetValueOrDefault(id);
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