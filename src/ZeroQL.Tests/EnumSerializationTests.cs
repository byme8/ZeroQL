using System.Text.Json;
using FluentAssertions;
using Xunit;
using ZeroQL.Json;

namespace ZeroQL.Tests;

public class EnumSerializationTests
{
    public EnumSerializationTests()
    {
        ZeroQLEnumJsonSerializersStore.Converters[typeof(Kinds)] =
            new ZeroQLEnumConverter<Kinds>(
                new Dictionary<string, Kinds>
                {
                    { "GOOD", Kinds.Good },
                    { "SUPER_GOOD", Kinds.SuperGood },
                    { "BAD", Kinds.Bad },
                    { "BAD1", Kinds.Bad1 },
                },
                new Dictionary<Kinds, string>
                {
                    { Kinds.Good, "GOOD" },
                    { Kinds.SuperGood, "SUPER_GOOD" },
                    { Kinds.Bad, "BAD" },
                    { Kinds.Bad1, "BAD1" },
                });
    }

    public static IEnumerable<object[]> SerializationData =>
        new List<object[]>
        {
            new object[] { new EnumContainer() { Kind = Kinds.Bad }, @"{""kind"":""BAD""}" },
            new object[] { new EnumContainer() { Kind = Kinds.Good }, @"{""kind"":""GOOD""}" },
            new object[] { new EnumContainer() { Kind = Kinds.SuperGood }, @"{""kind"":""SUPER_GOOD""}" },
            new object[] { new EnumContainer() { Kind = Kinds.Bad1 }, @"{""kind"":""BAD1""}" },
        };

    [Theory]
    [MemberData(nameof(SerializationData))]
    public void SerializationWorks(EnumContainer container, string expected)
    {
        var json = JsonSerializer.Serialize(container, ZeroQLJsonOptions.Options);
        json.Should().Be(expected);
    }

    public static IEnumerable<object[]> DeserializationData =>
        new List<object[]>
        {
            new object[] { @"{""kind"":""BAD""}", new EnumContainer() { Kind = Kinds.Bad }, },
            new object[] { @"{""kind"":""GOOD""}", new EnumContainer() { Kind = Kinds.Good }, },
            new object[] { @"{""kind"":""SUPER_GOOD""}", new EnumContainer() { Kind = Kinds.SuperGood }, },
            new object[] { @"{""kind"":""BAD1""}", new EnumContainer() { Kind = Kinds.Bad1 }, },
        };

    [Theory]
    [MemberData(nameof(DeserializationData))]
    public void DeserializationWorks(string json, EnumContainer expected)
    {
        var value = JsonSerializer.Deserialize<EnumContainer>(json, ZeroQLJsonOptions.Options);
        value.Should().Be(expected);
    }

    public record EnumContainer
    {
        public Kinds Kind { get; set; }
    }

    public enum Kinds
    {
        SuperGood,
        Good,
        Bad,
        Bad1
    }
}