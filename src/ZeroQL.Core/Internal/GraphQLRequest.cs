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
    public int Version { get; set; }
}

public class SHA256HashQueryExtension : GraphQLPersistedQueryExtension
{
    [JsonPropertyName("sha256Hash")]
    public string? SHA256Hash { get; set; }
}