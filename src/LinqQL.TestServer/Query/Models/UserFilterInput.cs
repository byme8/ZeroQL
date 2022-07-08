namespace LinqQL.TestServer.Query.Models;

public class UserFilterInput
{
    public UserKind UserKind { get; set; }

    public Page? Page { get; set; }
}