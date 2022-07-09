namespace LinqQL.Core;

public interface IGraphQLQueryProvider
{
    string Query { get; }
}

public class GraphQLResult<TData> : IGraphQLQueryProvider
{
    public string Query { get; set; }

    public TData Data { get; set; }

    public GraphQueryError[] Errors { get; set; }
}

public class GraphQLRequest
{
    public object? Variables { get; set; }
    public string Query { get; set; }
}

public class GraphQLResponse<TData>
{
    public TData? Data { get; set; }

    public GraphQueryError[]? Errors { get; set; }
}

public class GraphQueryError
{
    public string Message { get; set; }

    public ErrorLocation[] Locations { get; set; }
}

public class ErrorLocation
{
    public int Line { get; set; }
    public int Column { get; set; }
}