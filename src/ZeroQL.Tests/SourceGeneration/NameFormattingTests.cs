using FluentAssertions;
using Xunit;
using ZeroQL.SourceGenerators;

namespace ZeroQL.Tests.SourceGeneration;

public class NameFormattingTests
{
    [Theory]
    [InlineData("Good", "GOOD")]
    [InlineData("GoodBoy", "GOOD_BOY")]
    [InlineData("GoodBoyBadGirl", "GOOD_BOY_BAD_GIRL")]
    public void UpperCase(string name, string upperCaseName)
    {
        name.ToAllUpperCase().Should().Be(upperCaseName);
    }
}