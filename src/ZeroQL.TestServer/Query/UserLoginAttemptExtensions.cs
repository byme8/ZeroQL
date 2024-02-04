using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(User))]
public class UserLoginAttemptExtensions
{
    public UserLoginAttempt[] LoginAttempts([Parent]User user)
    {
        return new[]
        {
            new UserLoginAttempt { Time = DateTimeOffset.UtcNow, Success = true },
            new UserLoginAttempt { Time = DateTimeOffset.UtcNow, Success = false }
        };
    }
}

public class UserLoginAttempt
{
    public DateTimeOffset Time { get; set; }
    public bool Success { get; set; }
}