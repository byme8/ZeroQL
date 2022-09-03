using System.Diagnostics.CodeAnalysis;

namespace ZeroQL;

public class Unit
{
}

public interface IGraphQLResult
{
    string Query { get; }

    public GraphQueryError[] Errors { get; set; }
}

public class GraphQLResult<TData> : IGraphQLResult
{
    public GraphQLResult()
    {
        
    }
    
    public GraphQLResult(string query, TData? data, GraphQueryError[]? errors)
    {
        Query = query;
        Data = data;
        Errors = errors;
    }

    public string Query { get; set; }

    public TData? Data { get; set; }

    public GraphQueryError[]? Errors { get; set; }
}

public class GraphQLResponse<TData>
{
    public TData? Data { get; set; }

    public GraphQueryError[]? Errors { get; set; }
}

public class GraphQueryError
{
    public string Message { get; set; }

    public object[] Path { get; set; }
    
    public QLExtenstion Extensions { get; set; }
}

public class QLExtenstion
{
    public string Code { get; set; }
    public string Field { get; set; }
}