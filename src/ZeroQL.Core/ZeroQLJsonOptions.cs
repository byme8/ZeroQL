using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZeroQL.Extensions;

namespace ZeroQL;

public static class ZeroQLJsonOptions
{
    public static JsonSerializerOptions Options = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(new GraphQLEnumNamingPolicy()),
            new UploadJsonConverter(),
            new DateOnlyConverter()
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
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

    public class DateOnlyConverter : JsonConverter<DateOnly>
    {
        private readonly string serializationFormat = "yyyy-MM-dd";

        public override DateOnly Read(ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return DateOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer,
            DateOnly value,
            JsonSerializerOptions options)
        {
            var text = value.ToString(serializationFormat);
            writer.WriteStringValue(text);
        }
    }
}