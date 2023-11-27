using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroQL.Json;

public class ZeroQLConverter(Dictionary<Type, JsonConverter> converters) : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => converters.ContainsKey(typeToConvert);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return converters.GetValueOrDefault(typeToConvert);
    }
}