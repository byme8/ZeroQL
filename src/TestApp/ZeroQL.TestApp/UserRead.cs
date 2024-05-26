namespace ZeroQL.TestApp;

public class UserRead
{
    public ID Id { get; set; }
    
    public UserKind2 Kind2 { get; set; }
}

public enum UserKind2
{
    User,
    Admin
}
