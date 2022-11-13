using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroQL.Json;

public class ZeroQLEnumStringConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => ZeroQLEnumJsonSerializersStore.Converters.ContainsKey(typeToConvert);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (ZeroQLEnumJsonSerializersStore.Converters.TryGetValue(typeToConvert, out var converter))
        {
            return converter;
        }

        return null;
    }
}