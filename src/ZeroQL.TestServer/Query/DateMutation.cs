namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(Mutation))]
public class DateMutation
{
    public DateTime GetDateTime(DateTime dateTime) => dateTime;

    public DateTime?[]? GetDateTimes(DateTime?[]? dateTime) => dateTime;

    public DateTimeOffset GetDateTimeOffset(DateTimeOffset dateTimeOffset) => dateTimeOffset;

    public TimeSpan GetTimeSpan(TimeSpan timeSpan) => timeSpan;

    public DateOnly GetDateOnly(DateOnly dateOnly) => dateOnly;

    public TimeOnly GetTimeOnly(TimeOnly timeOnly) => timeOnly;
}