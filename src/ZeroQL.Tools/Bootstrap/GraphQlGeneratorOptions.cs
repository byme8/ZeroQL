using System;
using System.Collections.Generic;
using ZeroQL.Internal.Enums;
using ZeroQL.Schema;

namespace ZeroQL.Bootstrap;

public record GraphQlGeneratorOptions(string ClientNamespace, ClientVisibility Visibility)
{
    public string? ClientName { get; set; }

    public Dictionary<string, string>? Scalars { get; init; }

    public string AccessLevel => Visibility switch
    {
        ClientVisibility.Public => "public",
        ClientVisibility.Internal => "internal",
        _ => throw new ArgumentOutOfRangeException()
    };

    public bool? NetstandardCompatibility { get; set; }

    public string GetDefinitionFullTypeName(Definition definition)
        => $"global::{ClientNamespace}.{definition.Name}";
    
    public string GetDefinitionFullTypeName(string definition)
        => $"global::{definition}";
}