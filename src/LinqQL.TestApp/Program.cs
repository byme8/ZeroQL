
var context = new QueryContext<Query>(new Query(new UserQL("Jon", "Smith", new RoleQL(12, "Admin"))));
var result = context
    .Query(
        new { FirstId = 10, SecondId = 20 },
        static (a, q) => new
        {
            User = q.User(a.FirstId, o => new User(o.FirstName, o.LastName, o.Role(o => o.Name))),
            SecondUser = q.User(a.SecondId, o => new
            {
                o.FirstName
            })
        });

var firstUser = result.User;
var secondUser = result.SecondUser;


public record User(string FirstName, string LastName, string Role);

public class Query
{
    private UserQL userQl;

    public Query(UserQL userQl)
    {
        this.userQl = userQl;
    }


    public T User<T>(int id, Func<UserQL, T> selector)
    {
        return selector(userQl);
    }
}

public class UserQL
{
    private readonly RoleQL roleQl;

    public UserQL(string firstName, string lastName, RoleQL roleQl)
    {
        this.roleQl = roleQl;
        FirstName = firstName;
        LastName = lastName;
    }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public T Role<T>(Func<RoleQL, T> selector)
    {
        return selector(roleQl);
    }
}


public class RoleQL
{
    public RoleQL(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; set; }

    public string Name { get; set; }
}

public class QueryContext<TState>
{
    private readonly TState state;

    public QueryContext(TState state)
    {
        this.state = state;

    }

    public TResult Query<TArguments, TResult>(TArguments arguments, Func<TArguments, TState, TResult> query)
    {
        return query(arguments, state);
    }
}