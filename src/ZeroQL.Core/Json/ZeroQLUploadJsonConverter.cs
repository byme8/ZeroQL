using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroQL.Json;

public class ZeroQLUploadJsonConverter : JsonConverter<Upload>
{
    public override Upload Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => null;

    public override void Write(
        Utf8JsonWriter writer,
        Upload dateTimeValue,
        JsonSerializerOptions options) =>
        writer.WriteNullValue();
}