using FluentAssertions;
using ZeroQL.Extensions;

namespace ZeroQL.Tests;

public class TransformationTests
{
    [Theory]
    [InlineData("HELLO_WORLD", "HelloWorld")]
    [InlineData("HELLO__WORLD", "HelloWorld")]
    [InlineData("__HELLO__WORLD__", "HelloWorld")]
    public void ToPascalCase(string input, string expected)
    {
        var result = input.ToPascalCase();
        
        result.Should().Be(expected);
    }
    
    [Theory]
    [InlineData("HelloWorld", "HELLO_WORLD")]
    public void ToUpperCase(string input, string expected)
    {
        var result = input.ToUpperCase();
        
        result.Should().Be(expected);
    }
}