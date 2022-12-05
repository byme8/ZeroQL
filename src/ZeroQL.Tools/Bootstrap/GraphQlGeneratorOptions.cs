namespace ZeroQL.Bootstrap;

public record GraphQlGeneratorOptions(string ClientNamespace)
{
    public string ClientNamespace { get; } = ClientNamespace;
    public string? ClientName { get; set; }
}