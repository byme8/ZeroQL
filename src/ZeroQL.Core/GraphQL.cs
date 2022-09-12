namespace ZeroQL;

public abstract record GraphQL<TQuery, TResult>
{
    public abstract TResult Execute(TQuery query);
}