using HotChocolate.Types;

namespace LinqQL.TestServer;

public class Query
{

}

[ExtendObjectType(typeof(User))]
public class RoleGraphQLExtension
{
    public Role GetRole([Parent] User user)
    {
        return new Role()
        {
            Id = 42,
            Name = "Admin"
        };
    }
}

public class Role
{
    public int Id { get; set; }

    public string Name { get; set; }
}

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
            Id = id,
            FirstName = "Jon",
            LastName = "Smith"
        };
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

public class User
{
    public int Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }
}

public class TypesContainer
{
    public string Text { get; set; }

    public byte Value1 { get; set; }
    public byte? Value2 { get; set; }
    public short Value3 { get; set; }
    public short? Value4 { get; set; }
    public int Value5 { get; set; }
    public int? Value6 { get; set; }
    public long Value7 { get; set; }
    public long? Value8 { get; set; }
    public float Value9 { get; set; }
    public float? Value10 { get; set; }
    public double Value11 { get; set; }
    public double? Value12 { get; set; }
    public decimal Value13 { get; set; }
    public decimal? Value14 { get; set; }
    public DateTime Value15 { get; set; }
    public DateTime? Value16 { get; set; }
    public DateOnly Value17 { get; set; }
    public DateOnly? Value18 { get; set; }
    public Guid Value19 { get; set; }
    public Guid? Value20 { get; set; }
    public Guid[] Value21 { get; set; }
    public Guid[]? Value22 { get; set; }
    public List<Guid> Value23 { get; set; }
    public List<Guid>? Value24 { get; set; }
    public IEnumerable<Guid> Value25 { get; set; }
    public IEnumerable<Guid>? Value26 { get; set; }
    public Dictionary<string, string> Value27 { get; set; }
    public Dictionary<string, string>? Value28 { get; set; }
    public KeyValuePair<string, string> Value29 { get; set; }
    public KeyValuePair<string, string>? Value30 { get; set; }

}