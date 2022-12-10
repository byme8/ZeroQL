using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroQL.Json;

public class ZeroQLConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => ZeroQLJsonSerializersStore.Converters.ContainsKey(typeToConvert);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (ZeroQLJsonSerializersStore.Converters.TryGetValue(typeToConvert, out var converter))
        {
            return converter;
        }

        return null;
    }
}