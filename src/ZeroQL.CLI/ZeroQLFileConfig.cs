using Newtonsoft.Json;
#pragma warning disable CS8618

namespace ZeroQL.CLI;

public class ZeroQLFileConfig
{
    [JsonProperty("$schema")]
    public string Schema { get; set; }
    
    [JsonRequired]
    [JsonProperty("graphql")]
    public string GraphQL { get; set; }

    [JsonRequired]
    public string Namespace { get; set; }

    [JsonRequired]
    public string ClientName { get; set; }

    [JsonRequired]
    public string Output { get; set; }
}