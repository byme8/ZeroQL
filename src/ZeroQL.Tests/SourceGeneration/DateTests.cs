using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class DateTests : IntegrationTest
{
    [Theory]
    [InlineData("DateTime", "new DateTime(2042, 12, 11, 10, 9, 8, 7, DateTimeKind.Local)")]
    [InlineData("DateTimes", "new [] { new DateTimeOffset(2042, 12, 11, 10, 9, 8, 7, TimeSpan.FromHours(1)) }")]
    [InlineData("DateTimeOffset", "new DateTimeOffset(2042, 12, 11, 10, 9, 8, 7, TimeSpan.FromHours(1))")]
    [InlineData("TimeSpan", "new TimeSpan(7, 6, 5, 4, 3)")]
    [InlineData("DateOnly", "new DateOnly(2042, 12, 11)")]
    [InlineData("TimeOnly", "new TimeSpan(0, 2, 3, 4)")]
    public async Task Dates(string name, string creator)
    {
        var csharpQuery = $$"""
            var input = {{creator}};
            var response = await qlClient.Mutation(m => m.{{name}}(input));
        """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();
        await Verify(response)
            .UseParameters(name)
            .DontScrubDateTimes();
    }
}