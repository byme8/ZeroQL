using FluentAssertions;
using Microsoft.CodeAnalysis;
using ZeroQL.SourceGenerators;
using ZeroQL.SourceGenerators.Generator;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

[UsesVerify]
public class QueryTests : IntegrationTest
{
    [Fact]
    public async Task CompilationWorks()
    {
        await TestProject.Project.CompileToRealAssembly();
    }

    [Fact]
    public async Task SimpleQuery()
    {
        var graphqlQuery = @"query { me { firstName } }";
        var project = TestProject.Project;

        var result = (GraphQLResult<string>)await project.Validate(graphqlQuery);

        result.Data.Should().Be("Jon");
    }
    
    [Fact]
    public async Task QueryToWrongUrl()
    {
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", ("http://localhost:10000/graphql", "http://localhost:10000"));

        var result = await project.Execute();

        await Verify(result);
    }

    [Fact]
    public async Task SupportForAnonymousType()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName })";
        var graphqlQuery = @"query { me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task FieldsWithUpperCasingIsSupported()
    {
        var csharpQuery = "static q => q.MEWITHSUPPERCASING(o => new { o.FirstName })";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }

    [Fact]
    public async Task FieldsWithPascalCasingIsSupported()
    {
        var csharpQuery = "static q => q.MeWithPascalCasing(o => new { o.FirstName })";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }

    [Fact]
    public async Task NullableQueryHandled()
    {
        var csharpQuery = "static q => q.User(10, o => o.FirstName)";
        var graphqlQuery = @"query { user(id: 10) { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportForMultipleMembers()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName.Length })";
        var graphqlQuery = @"query { me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task FailsWhenAccessingVariablesOutLambdaScope()
    {
        var csharpQuery = "static q => q.Me(o => q.Me(o => o.FirstName))";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();

        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.DontUseOutScopeValues.Id);
    }

    [Fact]
    public async Task FailsOnOpenLambda()
    {
        var csharpQuery = "static q => q.Me(o => o)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();
        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.OpenLambdaIsNotAllowed.Id);
    }

    [Fact]
    public async Task FailsWhenMethodCalled()
    {
        var csharpQuery = "static q => q.Me(o => o.ToString())";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();
        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.OnlyFieldSelectorsAndFragmentsAreAllowed.Id);
    }

    [Fact]
    public async Task SupportNotStaticLambdaWithoutArguments()
    {
        var csharpQuery = "q => q.Me(o => o.FirstName)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();

        diagnostics
            .Where(o => o.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task FailsWhenLambdaIsNotStaticWithArgument()
    {
        var csharpQuery = "new { Id = 1}, (i, q) => q.Me(o => o.FirstName)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();

        diagnostics.Should()
            .Contain(o => o.Id == Descriptors.OnlyStaticLambda.Id);
    }

    [Fact]
    public async Task QueryPreviewGenerated()
    {
        var csharpQuery = "static q => new { Me = q.Me(o => new { o.FirstName }) }";
        var graphqlQuery = @"query { me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();
        var queryPreview = diagnostics
            .Where(o => o.Id == Descriptors.GraphQLQueryPreview.Id)
            .Select(o => o.GetMessage());

        queryPreview.Should().Contain(graphqlQuery);
    }

    [Fact]
    public async Task SupportsAnonymousTypeInQueryRoot()
    {
        var csharpQuery = "static q => new { Me = q.Me(o => new { o.FirstName }) }";
        var graphqlQuery = @"query { me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportsAnonymousTypeWithMultipleFields()
    {
        var csharpQuery = "static q => q.Me(o => new { o.FirstName, o.LastName })";
        var graphqlQuery = @"query { me { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact(Skip = "Figure out how to support this")]
    public async Task SupportsAnonymousTypeWithMultipleIdenticalFieldsInRootQuery()
    {
        var csharpQuery =
            "static q => new { Me1 = q.Me(o => new { o.FirstName, o.LastName }), Me2 = q.Me(o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"{ m1: me { firstName lastName } m2: me { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportsAnonymousTypeWithConstantArgumentQuery()
    {
        var csharpQuery = "static q => new { User = q.User(42, o => new { o.FirstName, o.LastName }) }";
        var graphqlQuery = @"query { user(id: 42) { firstName lastName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportForEnums()
    {
        var query = "q => q.Me(o => new { o.Id, o.UserKind })";
        var response = await TestProject.Project.Execute(query);
        
        await Verify(response);
    }
    
    [Fact]
    public async Task SupportForEnumsWithCast()
    {
        var query = "q => q.Me(o => new { Id = o.Id, UserKind = (UserKind)o.UserKind })";
        var response = await TestProject.Project.Execute(query);
        
        await Verify(response);
    }

    [Fact]
    public async Task SupportForEnumsWithCustomCasing()
    {
        var csharpQuery = "static q => q.UserKindPascals";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }

    [Fact]
    public async Task SupportForArray()
    {
        var arguments = "new { Filter = new UserFilterInput { UserKind = UserKind.Good } }";
        var csharpQuery = "static (i, q) => q.Users(i.Filter, 0,  10, o => o.FirstName)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, $"{arguments}, {csharpQuery}"));

        var response = await project.Execute();

        await Verify(response);
    }

    [Fact]
    public async Task SupportForArrayWithScalarElements()
    {
        var csharpQuery = "static q => q.UsersIds(UserKind.Good, 0, 10)";
        var graphqlQuery = @"query { usersIds(kind: GOOD, page: 0, size: 10)}";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportsQueryName()
    {
        var csharpQuery = "static q => q.Me(o => o.FirstName)";
        var graphqlQuery = @"query Me{ me { firstName } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, @"""Me"", " + csharpQuery));

        await project.Validate(graphqlQuery);
    }

    [Fact]
    public async Task SupportsOnlyLiteralQueryName()
    {
        var csharpQuery = "static q => q.Me(o => o.FirstName)";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.MeQuery, @"nameof(Execute), " + csharpQuery));

        var diagnostics = await project.ApplyAnalyzers();

        diagnostics.Should().Contain(o => o.Id == Descriptors.GraphQLQueryNameShouldBeLiteral.Id);
    }

    [Fact]
    public async Task SupportsExtensionsInsideError()
    {
        var csharpQuery = "Mutation(static m => m.DoError)";
        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullMeQuery, csharpQuery));

        var response = await project.Execute();
        await Verify(response);
    }

    [Fact]
    public async Task SupportsOptionalArguments()
    {
        var csharpQuery = """
                          var page = 0;
                          var filter = "test";
                          var response = await qlClient.Query(q => q.UsersIds(page: page, filter: filter));
                          """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }

    [Fact]
    public async Task ReportsLinqMethodsAsErrors()
    {
        var types = """
                    public record MyUser(string Id, string Name, string Username, MyLogin? LastLogin = null);

                    public record MyLogin(DateTimeOffset Time, bool Success);
                    """;

        var csharpQuery = """
                          var response = await qlClient.Query(static q => q
                                     .Me(o => new MyUser(
                                         o.Id,
                                         o.FirstName,
                                         o.LastName,
                                         o.LoginAttempts(oo => new MyLogin(
                                             oo.Time,
                                             oo.Success
                                         )).FirstOrDefault()
                                     )));
                          """;

        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            var project = await TestProject.Project
                .ReplacePartOfDocumentAsync("Program.cs",
                    (TestProject.PlaceToReplaceInClassProgram, types),
                    (TestProject.FullLine, csharpQuery));

            await project.Execute();
        });

        await Verify(exception.Message);
    }

    [Fact]
    public async Task SupportsOptionalArgumentsWithOnlySelector()
    {
        var csharpQuery = """
                          var response = await qlClient.Query(q => q.Users(selector: o => o.FirstName));
                          """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }

    [Fact]
    public async Task SupportsLocalStaticFunctionAsFragment()
    {
        var csharpQuery = """
                          var response = await qlClient.Query(static q => q.Me(GetFirstName));
                          static string GetFirstName(User user) => user.FirstName;
                          """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }

    [Fact]
    public async Task SupportForVariablesPassedViaClosure()
    {
        var csharpQuery = """
                          var userIds = new[] { 1 };
                          var response = await qlClient.Query(q => q.UsersByIds(userIds, o => o.FirstName));
                          """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }

    [Fact]
    public async Task NamedArgumentsAreSupported()
    {
        var csharpQuery = """
                              var cts = new CancellationTokenSource();
                              var response = await qlClient.Query(cancellationToken: cts.Token, query: static q => q.Me(o => o.FirstName));
                          """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();
        await Verify(response);
    }

    [Fact]
    public async Task CombinationNullableAndNonNullable()
    {
        var csharpQuery = """
                              var limit = new LimitInputZeroQL();
                              var response = await qlClient.Mutation(m => new
                              {
                                  Limit2 = m.AddLimitNullable(limit, o => o.Limit),
                                  Limit = m.AddLimit(limit, o => o.Limit)
                              });
                          """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        var csharpQuery2 = """
                               var limit = new LimitInputZeroQL();
                               var response = await qlClient.Mutation(m => new
                               {
                                   Limit = m.AddLimit(limit, o => o.Limit),
                                   Limit2 = m.AddLimitNullable(limit, o => o.Limit)
                               });
                           """;

        var project2 = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery2));

        var response2 = await project2.Execute();

        await Verify(new
        {
            response,
            response2
        });
    }

    [Fact]
    public async Task QueryDataIsReturnedWhenErrorHappens()
    {
        var csharpQuery = """
                          var response = await qlClient.Query(q => q
                            .ContainerWithoutError(o => new
                                {
                                    o.Value,
                                    Error = o.ContainerWithError(oo => oo.Value)
                                }));
                          """;

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.FullLine, csharpQuery));

        var response = await project.Execute();

        await Verify(response);
    }

    [Fact]
    public async Task LambdaModuleInitializerGenerated()
    {
        var result = await TestProject.Project
            .ApplyGenerator(new GraphQLLambdaIncrementalSourceGenerator());

        await Verify(result.GeneratedTrees.Select(o => o.GetText().ToString()));
    }
}