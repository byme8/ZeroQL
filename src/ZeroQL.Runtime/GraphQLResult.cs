using System.Collections.Generic;

namespace ZeroQL;

public class Unit
{
    public static Unit Default { get; } = new();
}

public interface IGraphQLResult
{
    string Query { get; }

    public GraphQueryError[]? Errors { get; }

    public Dictionary<string, object>? Extensions { get; }
}

public class GraphQLResult<TData> : IGraphQLResult
{
    public GraphQLResult()
    {
        
    }
    
    public GraphQLResult(string query, TData? data, GraphQueryError[]? errors, Dictionary<string, object>? extensions)
    {
        Query = query;
        Data = data;
        Errors = errors;
        Extensions = extensions;
    }

    public string Query { get; set; }

    public TData? Data { get; set; }

    public GraphQueryError[]? Errors { get; set; }

    public Dictionary<string, object>? Extensions { get; set; }
}

public record GraphQLResponse<TData>
{
    public string Query { get; set; }
    
    public TData? Data { get; set; }

    public GraphQueryError[]? Errors { get; set; }
    
    public Dictionary<string, object>? Extensions { get; set; }
}

public class GraphQueryError
{
    public string Message { get; set; }

    public object[] Path { get; set; }
    
    public Dictionary<string, object>? Extensions { get; set; }
}