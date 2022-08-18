using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(Mutation))]
public class UserGraphQLMutations
{
    public User AddUser(string firstName, string lastName)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName
        };

        return user;
    }
    
    public async Task<int> AddUserProfileImage(int userId, IFile file)
    {
        var stream = file.OpenReadStream();
        var streamReader = new StreamReader(stream);
        var data = await streamReader.ReadToEndAsync();

        return data.Length;
    }
}