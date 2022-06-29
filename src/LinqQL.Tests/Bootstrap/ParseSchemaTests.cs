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
        Csharp = GraphQLGenerator.ToCSharp(TestSchema.RawSchema, "TestApp");
        SyntaxTree = CSharpSyntaxTree.ParseText(Csharp);
    }

    public string Csharp { get; }

    public SyntaxTree SyntaxTree { get; }


    [Fact]
    public void QueryDetected()
    {
        var properties = new[]
        {
            (Name: "Admin", Type: "User?"),
            (Name: "User", Type: "User"),
            (Name: "Users", Type: "User[]")
        };

        var query = SyntaxTree.GetClass("Query");

        query.Members.OfType<MethodDeclarationSyntax>()
            .Select(o =>
            {
                var methodName = o.Identifier.ValueText;
                var genericName = (GenericNameSyntax)o.ParameterList.Parameters.Last().Type!;
                return (methodName, genericName.TypeArgumentList.Arguments.First().ToString());
            })
            .Should()
            .Contain(properties);
    }

    [Fact]
    public void UserDetected()
    {
        var propertiesNames = new[] { "FirstName", "LastName" };

        var query = SyntaxTree.GetClass("User");

        query.Should().NotBeNull();
        query.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(o => o.Identifier.ValueText).Should()
            .Contain(propertiesNames).And.HaveCount(2);
    }

    [Fact]
    public void FieldsHasArguments()
    {
        var query = SyntaxTree.GetClass("Query");
        var user = query.GetMethod("User");

        user.ParameterList.Parameters
            .Should()
            .Contain(o => o.Identifier.ValueText == "id" && o.Type.ToString() == "int");

    }

    [Fact]
    public void BackedFieldsHasJsonPropertyNames()
    {
        var query = SyntaxTree.GetClass("Query");
        var user = query.GetField("_User");

        user.AttributeLists
            .SelectMany(o => o.Attributes)
            .Should()
            .Contain(o => o.Name.ToString() == "JsonPropertyName" &&
                          o.ArgumentList.Arguments.First().Expression.ToString() == @"""User""");

    }
}