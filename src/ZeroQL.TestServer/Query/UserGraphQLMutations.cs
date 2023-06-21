using HotChocolate.Resolvers;
using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(Mutation))]
public class UserGraphQLMutations
{
    public User AddUser(string firstName, string lastName)
    {
        var user = new User
        {
            Id = 10,
            FirstName = firstName,
            LastName = lastName
        };

        return user;
    }

    public int DoError(IResolverContext context)
    {
        throw new GraphQLException(new Error("Error", extensions: new Dictionary<string, object?>
        {
            {"message", "This is an error"}
        }));
    }

    public async Task<int> AddUserProfileImage(int userId, IFile file)
    {
        var stream = file.OpenReadStream();
        var streamReader = new StreamReader(stream);
        var data = await streamReader.ReadToEndAsync();

        return data.Length;
    }

    public async Task<int> AddMyProfileImage(IFile file)
    {
        var stream = file.OpenReadStream();
        var streamReader = new StreamReader(stream);
        var data = await streamReader.ReadToEndAsync();

        return data.Length;
    }

    public class UserInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public IFile Avatar { get; set; }
    }

    public async Task<int> AddUsersInfo(UserInfo[] users)
    {
        var sum = 0;
        foreach (var user in users)
        {
            var stream = user.Avatar.OpenReadStream();
            var streamReader = new StreamReader(stream);
            var data = await streamReader.ReadToEndAsync();
            sum += data.Length;
        }

        return sum;
    }
    
    public class AddUsersInput
    {
        public UserInfo? User { get; set; }
    }
    
    public async Task<int> AddUsersInfoWithEmails(AddUsersInput input)
    {
        return 10;
    }
    
    public int AddUserKindPascal(UserKindPascal userKindPascal)
    {
        return (int)userKindPascal + 10;
    }
    
    public Limit_1 AddLimit(Limit_1 limit)
    {
        return limit;
    }
    
    public Limit_2 AddLimit2(Limit_2 limit)
    {
        return limit;
    }

    public Task<int> AddValues(
        string text,
        byte value1,
        byte? value2,
        short value3,
        short? value4,
        int value5,
        int? value6,
        long value7,
        long? value8,
        float value9,
        float? value10,
        double value11,
        double? value12,
        decimal value13,
        decimal? value14,
        DateTime value15,
        DateTime? value16,
        DateOnly value17,
        DateOnly? value18,
        Guid value19,
        Guid? value20,
        Guid[] value21,
        Guid[]? value22,
        List<Guid> value23,
        List<Guid>? value24,
        IEnumerable<Guid> value25,
        IEnumerable<Guid>? value26,
        Dictionary<string, string> value27,
        Dictionary<string, string>? value28,
        KeyValuePair<string, string> value29,
        KeyValuePair<string, string>? value30,
        DateTimeOffset value31,
        DateTimeOffset? value32
    )
    {
        return Task.FromResult(1);
    }
}