using ZeroQL.Internal.Enums;

namespace ZeroQL.Bootstrap;

public record GraphQlGeneratorOptions(string ClientNamespace, ClientVisibility Visibility)
{
    public string? ClientName { get; set; }
}