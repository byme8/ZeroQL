using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroQL.Json;

public static class ZeroQLJsonOptions
{
    public static JsonSerializerOptions Create() => new()
    {
        Converters =
        {
            new ZeroQLUploadJsonConverter(),
            new ZeroQLTimeSpanConverter(),
#if !NETSTANDARD
            new ZeroQLDateOnlyConverter(),
#endif
            new ZeroQLIDJsonConverter()
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}