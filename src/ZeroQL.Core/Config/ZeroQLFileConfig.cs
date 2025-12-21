using System.Text.Json.Serialization;
using ZeroQL.Core.Enums;

#pragma warning disable CS8618

namespace ZeroQL.Core.Config;

public class ZeroQLFileConfig
{
    /// <summary>
    /// Stub property to set schema url
    /// </summary>
    [JsonPropertyName("$schema")]
    public string Schema { get; set; }

    /// <summary>
    /// The path to the graphql schema file
    /// </summary>
    [JsonRequired]
    [JsonPropertyName("graphql")]
    public string GraphQL { get; set; }

    /// <summary>
    /// The namespace for generated client
    /// </summary>
    /// <example>
    /// UserService.GraphQL.Clients
    /// </example>
    [JsonRequired]
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; }

    /// <summary>
    /// The client name for the generated client
    /// </summary>
    /// <example>
    /// UserServiceGraphQLClient
    /// </example>
    [JsonRequired]
    [JsonPropertyName("clientName")]
    public string ClientName { get; set; }

    /// <summary>
    /// The visibility within the assembly for the generated client
    /// </summary>
    [JsonPropertyName("visibility")]
    public ClientVisibility? Visibility { get; set; }

    /// <summary>
    /// The warnings to ignore when generating the client
    /// </summary>
    [JsonPropertyName("warningsToIgnore")]
    public string[]? WarningsToIgnore { get; set; }

    /// <summary>
    /// The custom scalars to use when generating the client
    /// </summary>
    [JsonPropertyName("scalars")]
    public Dictionary<string, string>? Scalars { get; set; }

    /// <summary>
    /// The path to the output file
    /// </summary>
    /// <example>
    /// ./Generated/GraphQL.g.cs
    /// </example>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>
    /// Enables netstandard compatibility during generation
    /// </summary>
    [JsonPropertyName("netstandardCompatibility")]
    public bool? NetstandardCompatibility { get; set; }

    /// <summary>
    /// The URL to pull the schema from
    /// </summary>
    /// <example>
    /// https://server.com/graphql
    /// </example>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Timeout in seconds for downloading the schema
    /// </summary>
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }
}
