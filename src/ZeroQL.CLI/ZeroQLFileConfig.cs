using Newtonsoft.Json;
using ZeroQL.Internal.Enums;

#pragma warning disable CS8618

namespace ZeroQL.CLI;

public class ZeroQLFileConfig
{
    /// <summary>
    /// Stub property to set schema url
    /// </summary>
    [JsonProperty("$schema")] public string Schema { get; set; }

    /// <summary>
    /// The path to the graphql schema file
    /// </summary>
    [JsonRequired]
    [JsonProperty("graphql")]
    public string GraphQL { get; set; }

    /// <summary>
    /// The namespace for generated client
    /// </summary>
    /// <example>
    /// UserService.GraphQL.Clients
    /// </example>
    [JsonRequired] public string Namespace { get; set; }

    /// <summary>
    /// The client name for the generated client
    /// </summary>
    /// <example>
    /// UserServiceGraphQLClient
    /// </example>
    [JsonRequired] public string ClientName { get; set; }

    /// <summary>
    /// The visibility within the assembly for the generated client
    /// </summary>
    public ClientVisibility? Visibility { get; set; }

    /// <summary>
    /// The custom scalars to use when generating the client
    /// </summary>
    public KeyValuePair<string, string>[]? CustomScalars { get; set; }
    
    /// <summary>
    /// The path to the output file
    /// </summary>
    /// <example>
    /// ./Generated/GraphQL.g.cs
    /// </example>
    [JsonRequired] public string Output { get; set; }
}