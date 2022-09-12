using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroQL.Json;

public static class ZeroQLJsonOptions
{
    public static JsonSerializerOptions Options = new()
    {
        Converters =
        {
            new ZeroQLEnumStringConverter(),
            new ZeroQLUploadJsonConverter(),
            new ZeroQLDateOnlyConverter()
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}