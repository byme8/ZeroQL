namespace ZeroQL.TestServer.Query.Models;

public class User
{
    public int Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public UserKind UserKind { get; set; }
}