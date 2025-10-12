using AwesomeAssertions;
using ZeroQL.Extensions;

namespace ZeroQL.Tests.SourceGeneration;

public class NameFormattingTests
{
    [Theory]
    [InlineData("_123", "_123")]
    [InlineData("GOOD", "Good")]
    [InlineData("GOOD_BOY", "GoodBoy")]
    [InlineData("GOOD_BOY_BAD_GIRL", "GoodBoyBadGirl")]
    public void PascalCase(string name, string upperCaseName)
    {
        name.ToPascalCase().Should().Be(upperCaseName);
    }
}