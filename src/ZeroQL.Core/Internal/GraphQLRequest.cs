using System.Text.Json.Serialization;

namespace ZeroQL.Internal;

public class GraphQLRequest
{
    public object? Variables { get; set; }
    public string? Query { get; set; }

    public GraphQLRequestExtensions? Extensions { get; set; }
}

public class GraphQLRequestExtensions
{
    public GraphQLPersistedQueryExtension? PersistedQuery { get; set; }
}

public class GraphQLPersistedQueryExtension
{
    [JsonPropertyName("sha256Hash")]
    public string? SHA256Hash { get; set; }
    
    public int Version { get; set; }
}