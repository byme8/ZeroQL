using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroQL.Json;

public class ZeroQLEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : Enum
{
    private readonly Dictionary<string, TEnum> toEnum;
    private readonly Dictionary<TEnum, string> fromEnum;

    public ZeroQLEnumConverter(Dictionary<string, TEnum> toEnum, Dictionary<TEnum, string> fromEnum)
    {
        this.toEnum = toEnum;
        this.fromEnum = fromEnum;
    }

    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value == null)
        {
            return default;
        }

        if (toEnum.TryGetValue(value, out var result))
        {
            return result;
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        var valueString = fromEnum[value];
        writer.WriteStringValue(valueString);
    }
}