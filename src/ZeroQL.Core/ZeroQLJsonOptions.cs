using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZeroQL.Core.Extensions;

namespace ZeroQL.Core;

public static class ZeroQLJsonOptions
{
    public static JsonSerializerOptions Options = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(new GraphQLEnumNamingPolicy()),
            new UploadJsonConverter()
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };


    public class GraphQLEnumNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToUpperCase();
        }
    }

    public class UploadJsonConverter : JsonConverter<Upload>
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
}