using FluentAssertions;
using LinqQL.Core.Bootstrap;
using LinqQL.Core.Extensions;
using LinqQL.Tests.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace LinqQL.Tests.Bootstrap;

public class ParseSchemaTests
{
    public ParseSchemaTests()
    {
        Csharp = GraphQLGenerator.ToCSharp(TestSchema.RawSchema, "TestApp", "GraphQLClient");
        SyntaxTree = CSharpSyntaxTree.ParseText(Csharp);
    }

    public string Csharp { get; }

    public SyntaxTree SyntaxTree { get; }


    [Fact]
    public void QueryDetected()
    {
        var properties = new[]
        {
            "T Me<T>(Func<User, T> selector)",
            "T[] Users<T>(UserFilterInput filter, int page, int size, Func<User, T> selector)",
            "UserKind[] UserKinds()",
            "T[][] UsersMatrix<T>(Func<User, T> selector)",
            "T[] UsersByKind<T>(UserKind kind, int page, int size, Func<User, T> selector)",
            "int[] UsersIds(UserKind kind, int page, int size)",
            "T User<T>(int id, Func<User, T> selector)",
            "T? Admin<T>(int id, Func<User?, T> selector)",
            "T Container<T>(Func<TypesContainer, T> selector)",
        };

        var query = SyntaxTree.GetClass("Query");

        query.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(o =>
            {
                var returnType = o.ReturnType.ToString();
                var methodName = o.Identifier.ToString();
                var genericArguments = o.TypeParameterList?.ToString();
                var genericName = o.ParameterList.ToString();
                return $@"{returnType} {methodName}{genericArguments}{genericName}";
            })
            .Should()
            .Contain(properties);
    }
    
    [Fact]
    public void MutationDetected()
    {
        var properties = new[]
        {
            "T AddUser<T>(string firstName, string lastName, Func<User, T> selector)"
        };

        var query = SyntaxTree.GetClass("Mutation");

        query.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(o =>
            {
                var returnType = o.ReturnType.ToString();
                var methodName = o.Identifier.ToString();
                var genericArguments = o.TypeParameterList?.ToString();
                var genericName = o.ParameterList.ToString();
                return $@"{returnType} {methodName}{genericArguments}{genericName}";
            })
            .Should()
            .Contain(properties);
    }
    
    [Fact]
    public void ClientGenerated()
    {
        var properties = new[]
        {
            "UserKind UserKind",
            "PageInput? Page"
        };

        var query = SyntaxTree.GetClass("UserFilterInput");

        query.Members
            .OfType<MethodDeclarationSyntax>()
            .Should()
            .BeEmpty();

        query.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(o => $"{o.Type.ToString()} {o.Identifier.ValueText}")
            .Should()
            .Contain(properties);
    }

    [Fact]
    public void InputsDetected()
    {
        var properties = new[]
        {
            "UserKind UserKind",
            "PageInput? Page"
        };

        var query = SyntaxTree.GetClass("UserFilterInput");

        query.Members
            .OfType<MethodDeclarationSyntax>()
            .Should()
            .BeEmpty();

        query.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(o => $"{o.Type.ToString()} {o.Identifier.ValueText}")
            .Should()
            .Contain(properties);
    }

    [Fact]
    public void UserDetected()
    {
        var propertiesNames = new[] { "Id", "FirstName", "LastName", "UserKind" };

        var query = SyntaxTree.GetClass("User");

        query.Should().NotBeNull();
        query.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(o => !o.Identifier.ValueText.StartsWith("__"))
            .Select(o => o.Identifier.ValueText).Should()
            .Contain(propertiesNames).And.HaveCount(4);
    }

    [Fact]
    public void FieldsHasArguments()
    {
        var query = SyntaxTree.GetClass("Query");
        var user = query.GetMethod("User");

        user.ParameterList.Parameters
            .Should()
            .Contain(o => o.Identifier.ValueText == "id" && o.Type!.ToString() == "int");

    }

    [Fact]
    public void BackedFieldsHasJsonPropertyNames()
    {
        var query = SyntaxTree.GetClass("Query");
        var user = query.GetProperty("__User");

        user.AttributeLists
            .SelectMany(o => o.Attributes)
            .Should()
            .Contain(o => o.Name.ToString() == "JsonPropertyName" &&
                          o.ArgumentList!.Arguments.First().Expression.ToString() == @"""User""");

    }
}