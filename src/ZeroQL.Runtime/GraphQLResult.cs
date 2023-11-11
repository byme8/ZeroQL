using System.Collections.Generic;
using MessagePack;

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

[MessagePackObject]
public record GraphQLResponse<TData>
{
    [IgnoreMember]
    public string Query { get; set; }
 
    [Key("data")]
    public TData? Data { get; set; }

    [Key("errors")]
    public GraphQueryError[]? Errors { get; set; }
    
    [Key("extensions")]
    public Dictionary<string, object>? Extensions { get; set; }
}

[MessagePackObject]
public class GraphQueryError
{
    [Key("message")]
    public string Message { get; set; }

    [Key("locations")]
    public object[] Path { get; set; }
    
    [Key("path")]
    public Dictionary<string, object>? Extensions { get; set; }
}