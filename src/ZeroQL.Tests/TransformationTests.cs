using AwesomeAssertions;
using ZeroQL.Extensions;

namespace ZeroQL.Tests;

public class TransformationTests
{
    [Theory]
    [InlineData("HELLO_WORLD", "HelloWorld")]
    [InlineData("HELLO__WORLD", "HelloWorld")]
    [InlineData("__HELLO__WORLD__", "HelloWorld")]
    [InlineData("Hello", "Hello")]
    [InlineData("hello", "Hello")]
    public void ToPascalCase(string input, string expected)
    {
        var result = input.ToPascalCase();
        
        result.Should().Be(expected);
    }
}