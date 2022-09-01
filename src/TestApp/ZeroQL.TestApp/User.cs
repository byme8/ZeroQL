namespace ZeroQL.TestApp.Models;

public record UserModel
{
    public UserModel()
    {
        
    }
    
    public UserModel(string firstName, string lastName, string role)
    {
        FirstName = firstName;
        LastName = lastName;
        Role = role;
    }

    public string FirstName
    {
        get;
        init;
    }

    public string LastName
    {
        get;
        init;
    }

    public string Role
    {
        get;
        init;
    }
}