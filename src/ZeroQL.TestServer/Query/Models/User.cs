namespace ZeroQL.TestServer.Query.Models;

public class User
{
    [ID("User")]
    public int Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public UserKind UserKind { get; set; }
    
    public TypeKind Kind { get; set; } = TypeKind.Object;

    public static User Create() => new User()
    {
        Id = 1,
        FirstName = "John",
        LastName = "Smith",
        UserKind = UserKind.Good
    };
}