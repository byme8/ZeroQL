using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

[UsesVerify]
public class DateTests : IntegrationTest
{
    [Theory]
    [InlineData("DateTime", "new DateTime(2042, 12, 11, 10, 9, 8, 7)")]
    [InlineData("DateTimes", "new [] { new DateTimeOffset(2042, 12, 11, 10, 9, 8, 7, TimeSpan.FromHours(1)) }")]
    [InlineData("DateTimeOffset", "new DateTimeOffset(2042, 12, 11, 10, 9, 8, 7, TimeSpan.FromHours(1))")]
    [InlineData("TimeSpan", "new TimeSpan(7, 6, 5, 4, 3)")]
    [InlineData("DateOnly", "new DateOnly(2042, 12, 11)")]
    [InlineData("TimeOnly", "new TimeSpan(0, 2, 3, 4)")]
    public async Task Dates(string name, string creator)
    {
        var csharpQuery = $$"""
            var input = new { Input = {{creator}} };
            var response = await qlClient.Mutation(input, static (i, m) => m.{{name}}(i.Input));
        """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();
        await Verify(response)
            .UseParameters(name)
            .DontScrubDateTimes();
    }
}