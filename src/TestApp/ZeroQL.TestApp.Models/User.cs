namespace ZeroQL.TestApp.Models;

public record UserModal
{
    public UserModal()
    {
        
    }
    
    public UserModal(string firstName, string lastName, string role)
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