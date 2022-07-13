using FluentAssertions;
using Xunit;
using ZeroQL.Core.Extensions;

namespace ZeroQL.Tests.SourceGeneration;

public class NameFormattingTests
{
    [Theory]
    [InlineData("Good", "GOOD")]
    [InlineData("GoodBoy", "GOOD_BOY")]
    [InlineData("GoodBoyBadGirl", "GOOD_BOY_BAD_GIRL")]
    public void UpperCase(string name, string upperCaseName)
    {
        name.ToUpperCase().Should().Be(upperCaseName);
    }
    
    [Theory]
    [InlineData("GOOD", "Good")]
    [InlineData("GOOD_BOY", "GoodBoy")]
    [InlineData("GOOD_BOY_BAD_GIRL", "GoodBoyBadGirl")]
    public void PascalCase(string name, string upperCaseName)
    {
        name.ToPascalCase().Should().Be(upperCaseName);
    }
}