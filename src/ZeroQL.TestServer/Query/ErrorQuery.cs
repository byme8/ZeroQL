namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(Query))]
public class ErrorQuery
{
    public ContainerWithoutError? GetContainerWithoutError() => new ContainerWithoutError { Value = "Hello" };
}

[ExtendObjectType(typeof(ContainerWithoutError))]
public class ErrorQueryWithError
{
    public ContainerWithError? GetContainerWithError() => throw new GraphQLException("Error");
}

public class ContainerWithoutError
{
    public string Value { get; set; }
}

public class ContainerWithError
{
    public string Value { get; set; }
}