using System.Text.Json.Serialization;
using ZeroQL.Core.Config;
using ZeroQL.Core.Enums;

namespace ZeroQL.Config;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true,
    AllowTrailingCommas = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ZeroQLFileConfig))]
[JsonSerializable(typeof(ClientVisibility))]
public partial class ZeroQLJsonContext : JsonSerializerContext;
