using Newtonsoft.Json;
using ZeroQL.Internal.Enums;

#pragma warning disable CS8618

namespace ZeroQL.CLI;

public class ZeroQLFileConfig
{
    [JsonProperty("$schema")] public string Schema { get; set; }

    [JsonRequired]
    [JsonProperty("graphql")]
    public string GraphQL { get; set; }

    [JsonRequired] public string Namespace { get; set; }

    [JsonRequired] public string ClientName { get; set; }

    public ClientVisibility? Visibility { get; set; }

    [JsonRequired] public string Output { get; set; }
}