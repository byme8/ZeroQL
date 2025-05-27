using FluentAssertions;
using Xunit;
using ZeroQL.SourceGenerators;
using ZeroQL.SourceGenerators.Analyzers;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class VariablesTests : IntegrationTest
{
    [Fact]
    public async Task SupportsPassedArgumentInQuery()
    {
        var csharpQuery = "static (i, q) => q.User(i.Id, o => o.Id)";
        var graphqlQuery = @"query ($id: Int!) { user(id: $id) { id } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, "new { Id = 2 }, " + csharpQuery));

        var response = (GraphQLResult<ID>)await project.Validate(graphqlQuery);

        response.Data.Should().Be(new ID("2"));
    }

    [Fact]
    public async Task SupportsAnonymousTypeWithArgumentQuery()
    {
        var csharpQuery = "static (i, q) => new { User = q.User(i.Id, o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"query ($id: Int!) { user(id: $id) { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, "new { Id = 42 }, " + csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportsPassedArgumentInNamedQuery()
    {
        var csharpQuery = "static (i, q) => q.User(i.Id, o => o.Id)";
        var graphqlQuery = @"query TestQuery($id: Int!) { user(id: $id) { id } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (TestProject.MeQuery, "\"TestQuery\", new { Id = 1 }, " + csharpQuery));

        var result = (GraphQLResult<ID>)await project.Validate(graphqlQuery);

        result.Data.Should().Be(new ID("1"));
    }

    [Fact]
    public async Task SupportsArraysAsArguments()
    {
        var csharpQuery = "static (i, q) => q.UsersByIds(i.Ids, o => o.Id)";
        var graphqlQuery = @"query TestQuery($ids: [Int!]!) { usersByIds(ids: $ids) { id } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (TestProject.MeQuery, "\"TestQuery\", new { Ids = new int [] { 1, 2 }  }, " + csharpQuery));

        var result = (GraphQLResult<ID[]>)await project.Validate(graphqlQuery);

        result.Data.Should().Contain(new ID[] { 1, 2 });
    }

    [Fact]
    public async Task SupportsDifferentTypes()
    {
        var csharpQuery = @"Mutation(new
            {
                Text = (string)""Hello World"",
                Value1 = (byte)1,
                Value2 = (byte?)1,
                Value3 = (short)1,
                Value4 = (short?)1,
                Value5 = (int)1,
                Value6 = (int?)1,
                Value7 = (long)1,
                Value8 = (long?)1,
                Value9 = (float)1,
                Value10 = (float?)1,
                Value11 = (double)1,
                Value12 = (double?)1,
                Value13 = (decimal)1,
                Value14 = (decimal?)1,
                Value15 = (DateTime)DateTime.Now,
                Value16 = (DateTime?)DateTime.Now,
                Value17 = (DateOnly)DateOnly.FromDateTime(DateTime.Now),
                Value18 = (DateOnly?)DateOnly.FromDateTime(DateTime.Now),
                Value19 = (Guid)Guid.NewGuid(),
                Value20 = (Guid?)Guid.NewGuid(),
                Value21 = (Guid[])new Guid[] { Guid.NewGuid() },
                Value22 = (Guid[]?)new Guid[] { Guid.NewGuid() },
                Value23 = (Guid[])new List<Guid> { Guid.NewGuid() }.ToArray(),
                Value24 = (Guid[]?)new List<Guid> { Guid.NewGuid() }.ToArray(),
                Value25 = (Guid[])new List<Guid> { Guid.NewGuid() }.ToArray(),
                Value26 = (Guid[]?)new List<Guid> { Guid.NewGuid() }.ToArray(),
                Value27 = new[] { new KeyValuePairOfStringAndStringInput() { Key = ""Hello"", Value = ""Value"" } },
                Value28 = new[] { new KeyValuePairOfStringAndStringInput() { Key = ""Hello"", Value = ""Value"" } },
                Value29 = new KeyValuePairOfStringAndStringInput() { Key = ""Hello"", Value = ""Value"" },
                Value30 = new KeyValuePairOfStringAndStringInput() { Key = ""Hello"", Value = ""Value"" },
                Value31 = (DateTimeOffset)DateTimeOffset.Now,
                Value32 = (DateTimeOffset?)DateTimeOffset.Now,
            },
            static (i, o) => o.SetValues(
                i.Text,
                i.Value1,
                i.Value2,
                i.Value3,
                i.Value4,
                i.Value5,
                i.Value6,
                i.Value7,
                i.Value8,
                i.Value9,
                i.Value10,
                i.Value11,
                i.Value12,
                i.Value13,
                i.Value14,
                i.Value15,
                i.Value16,
                i.Value17,
                i.Value18,
                i.Value19,
                i.Value20,
                i.Value21,
                i.Value22,
                i.Value23,
                i.Value24,
                i.Value25,
                i.Value26,
                i.Value27,
                i.Value28,
                i.Value29,
                i.Value30,
                i.Value31,
                i.Value32))";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullMeQuery, csharpQuery));

        var result = (GraphQLResult<int>)await project.Execute();

        await Verify(result);
    }

    [Fact]
    public async Task SupportForEnumsAsArgument()
    {
        var csharpQuery = "static q => q.UsersByKind(UserKind.Bad, 0, 10, o => o.FirstName)";
        var graphqlQuery = @"query { usersByKind(kind: BAD, page: 0, size: 10) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task VariablesCanBeSeparate()
    {
        var csharpQuery = "variables, static (i, q) => q.User(i.Id, o => o.FirstName)";
        var graphqlQuery = @"query ($id: Int!) { user(id: $id) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (TestProject.PlaceToReplace, "var variables = new { Id = 1 };"),
                (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task VariablesCanNotBeClassMembers()
    {
        var property = "public static int Id { get; set; } = 1;";
        var csharpQuery = "q => q.User(Id, o => o.FirstName)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs",
                (TestProject.PlaceToReplaceInClassProgram, property),
                (TestProject.MeQuery, csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();

        diagnostics.Select(o => o.Id).Should().Contain(Descriptors.GraphQLVariableShouldBeLocal.Id);
    }

    [Fact]
    public async Task VariablesCanNotBeNewInstanceCreation()
    {
        var csharpQuery = "var response = await qlClient.Query(q => q.User(new int(), o => o.FirstName))";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();

        diagnostics.Select(o => o.Id).Should().Contain(Descriptors.GraphQLVariableExpected.Id);
    }

    [Fact]
    public async Task VariablesCanBeFunctionArgument()
    {
        var csharpQuery = """
            var id = 1;
            var response = await ExecuteLocal(id);
            
            async Task<GraphQLResult<string>> ExecuteLocal(int idParam) => await qlClient.Query(q => q.User(idParam, o => o.FirstName));
            """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }
    
    [Fact]
    public async Task UnusedVariablesAreIgnored()
    {
        var csharpQuery = """
            var id = 1;
            var filter = "test";
            var response = await qlClient.Query(q => q.User(id, o => o.FirstName));
            """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }
}