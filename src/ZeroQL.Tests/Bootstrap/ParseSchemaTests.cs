using FluentAssertions;
using Microsoft.Build.Logging.StructuredLogger;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Bootstrap;
using ZeroQL.Extensions;
using ZeroQL.Internal.Enums;
using ZeroQL.Tests.Core;
using Task = System.Threading.Tasks.Task;

namespace ZeroQL.Tests.Bootstrap;

[UsesVerify]
public class ParseSchemaTests
{
    public ParseSchemaTests()
    {
        var options = new GraphQlGeneratorOptions("GraphQLClient", ClientVisibility.Public)
        {
            ClientName = "TestApp"
        };

        Csharp = GraphQLGenerator.ToCSharp(TestSchema.RawSchema, options);
        SyntaxTree = CSharpSyntaxTree.ParseText(Csharp);
    }

    public string Csharp { get; }

    public SyntaxTree SyntaxTree { get; }

    [Fact]
    public async Task PublicClient()
    {
        var options = new GraphQlGeneratorOptions("GraphQLClient", ClientVisibility.Public)
        {
            ClientName = "TestApp"
        };

        var graphql = GraphQLGenerator.ToCSharp(TestSchema.RawSchema, options);

        var hashCodeLine = graphql.GetLines().First();
        await Verify(graphql)
            .Track(hashCodeLine);
    }
    
    [Fact]
    public async Task InternalClient()
    {
        var options = new GraphQlGeneratorOptions("GraphQLClient", ClientVisibility.Internal)
        {
            ClientName = "TestApp"
        };

        var graphql = GraphQLGenerator.ToCSharp(TestSchema.RawSchema, options);

        var hashCodeLine = graphql.GetLines().First();
        await Verify(graphql)
            .Track(hashCodeLine);
    }

    [Fact]
    public void QueryDetected()
    {
        var methods = new[]
        {
            "T Me<T>(Func<User, T> selector)",
            "T[] Users<T>(UserFilterInput filter, int page, int size, Func<User, T> selector)",
            "T[][] UsersMatrix<T>(Func<User, T> selector)",
            "T[] UsersByKind<T>(UserKind kind, int page, int size, Func<User, T> selector)",
            "int[] UsersIds(UserKind kind, int page, int size)",
            "T? User<T>(int id, Func<User, T> selector)",
            "T? Admin<T>(int id, Func<User, T> selector)",
            "T Container<T>(Func<TypesContainer, T> selector)"
        };

        var properties = new[]
        {
            "UserKind[] UserKinds"
        };

        var query = SyntaxTree.GetClass("Query")!;

        query.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(o => o.AttributeLists
                .SelectMany(list => list.Attributes)
                .Any(attribute => attribute.Name.ToString() == ZeroQLGenerationInfo.GraphQLFieldSelectorAttribute))
            .Select(o =>
            {
                var returnType = o.ReturnType.ToString();
                var methodName = o.Identifier.ToString();
                var genericArguments = o.TypeParameterList?.ToString();
                var genericName = o.ParameterList.ToString();
                return $@"{returnType} {methodName}{genericArguments}{genericName}";
            })
            .Should()
            .Contain(methods);

        query.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(o => !o.Identifier.ValueText.StartsWith("__"))
            .Select(o =>
            {
                var returnType = o.Type.ToString();
                var name = o.Identifier.Text;
                return $@"{returnType} {name}";
            })
            .Should()
            .Contain(properties);
    }

    [Fact]
    public void TypeContainerDetected()
    {
        var properties = new[]
        {
            "string Text",
            "byte Value1",
            "byte? Value2",
            "short Value3",
            "short? Value4",
            "int Value5",
            "int? Value6",
            "long Value7",
            "long? Value8",
            "double Value9",
            "double? Value10",
            "double Value11",
            "double? Value12",
            "decimal Value13",
            "decimal? Value14",
            "DateTimeOffset Value15",
            "DateTimeOffset? Value16",
            "DateOnly Value17",
            "DateOnly? Value18",
            "Guid Value19",
            "Guid? Value20",
            "Guid[] Value21",
            "Guid[]? Value22",
            "Guid[] Value23",
            "Guid[]? Value24",
            "Guid[] Value25",
            "Guid[]? Value26"
        };

        var query = SyntaxTree.GetClass("TypesContainer")!;

        query.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(o =>
            {
                var returnType = o.Type.ToString();
                var name = o.Identifier.Text.ToString();
                return $@"{returnType} {name}";
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

        var query = SyntaxTree.GetClass("Mutation")!;

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

        var query = SyntaxTree.GetClass("UserFilterInput")!;

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

        var query = SyntaxTree.GetClass("UserFilterInput")!;

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

        var query = SyntaxTree.GetClass("User")!;

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
        var query = SyntaxTree.GetClass("Query")!;
        var user = query.GetMethod("User");

        user.ParameterList.Parameters
            .Should()
            .Contain(o => o.Identifier.ValueText == "id" && o.Type!.ToString() == "int");
    }

    [Fact]
    public void BackedFieldsHasJsonPropertyNames()
    {
        var query = SyntaxTree.GetClass("Query")!;
        var user = query.GetProperty("__User");

        user.AttributeLists
            .SelectMany(o => o.Attributes)
            .Should()
            .Contain(o => o.Name.ToString() == "JsonPropertyName" &&
                          o.ArgumentList!.Arguments.First().Expression.ToString() == @"""user""");
    }

    [Fact]
    public void EnumJsonConvertersGenerated()
    {
        var initializer = SyntaxTree.GetClass("JsonConvertersInitializers")!;

        initializer.Should().NotBeNull();
    }
    
    [Fact]
    public async Task EnumWithCustomNaming()
    {
        var initializer = SyntaxTree.GetEnum("UserKindPascal")!;

        await Verify(initializer.ToFullString());
    }

    [Fact]
    public void SchemaWithoutMutationHandledProperly()
    {
        var rawSchema = TestSchema.RawSchema
            .Replace("mutation: Mutation", "")
            .Replace("type Mutation", "type FakeMutation");

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        var clientClass = syntaxTree.GetClass("GraphQLClient")!;
        clientClass.Should().NotBeNull();

        var name = (QualifiedNameSyntax)clientClass.BaseList!.Types[0].Type;
        var genericName = (GenericNameSyntax)name.Right;
        genericName.TypeArgumentList.Arguments[1].ToString().Should().Be("ZeroQL.Unit");
    }

    [Fact]
    public void SchemaWithoutQueryHandledProperly()
    {
        var rawSchema = TestSchema.RawSchema
            .Replace("query: Query", "")
            .Replace("type Query", "type FakeQuery");

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        var clientClass = syntaxTree.GetClass("GraphQLClient")!;
        clientClass.Should().NotBeNull();

        var name = (QualifiedNameSyntax)clientClass.BaseList!.Types[0].Type;
        var genericName = (GenericNameSyntax)name.Right;
        genericName.TypeArgumentList.Arguments[0].ToString().Should().Be("ZeroQL.Unit");
    }

    [Fact]
    public void SchemaWithRenamedQueryHandledProperly()
    {
        var rawSchema = TestSchema.RawSchema
            .Replace("query: Query", "query: Queries")
            .Replace("type Query", "type Queries");

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        syntaxTree.GetClass("Query").Should().BeNull();
        syntaxTree.GetClass("Queries").Should().NotBeNull();

        var clientClass = syntaxTree.GetClass("GraphQLClient")!;
        clientClass.Should().NotBeNull();

        var name = (QualifiedNameSyntax)clientClass.BaseList!.Types[0].Type;
        var genericName = (GenericNameSyntax)name.Right;
        genericName.TypeArgumentList.Arguments[0].ToString().Should().Be("Queries");
    }

    [Fact]
    public void SchemaWithRenamedMutationHandledProperly()
    {
        var rawSchema = TestSchema.RawSchema
            .Replace("mutation: Mutation", "mutation: Mutations")
            .Replace("type Mutation", "type Mutations");

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        syntaxTree.GetClass("Mutation").Should().BeNull();
        syntaxTree.GetClass("Mutations").Should().NotBeNull();
        var clientClass = syntaxTree.GetClass("GraphQLClient")!;
        clientClass.Should().NotBeNull();

        var name = (QualifiedNameSyntax)clientClass.BaseList!.Types[0].Type;
        var genericName = (GenericNameSyntax)name.Right;
        genericName.TypeArgumentList.Arguments[1].ToString().Should().Be("Mutations");
    }

    [Fact]
    public void UserDefinedScalarIsDetected()
    {
        SyntaxTree.GetClass("Query")?
            .GetProperty("Instant")
            .Should().NotBeNull();
    }

    [Fact]
    public void DefaultValuesForInputObjects()
    {
        const string clientClassName = "Book";
        var rawSchema = @"
schema {
  mutation: Mutation
}

input " + clientClassName + @" {
  bookId: ID! = 1
  pages: int! = 100
  price: float! = 1.99
  title: string = ""The Hobbit""
  archive: bool = true
  
}

type Mutation {
  addBook(input: Book): int
}
";

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        var clientClass = syntaxTree.GetClass(clientClassName)!;
        clientClass.Should().NotBeNull();

        var bookIdProperty = clientClass.GetProperty("BookId");
        VerifyProperty(bookIdProperty, SyntaxKind.StringLiteralExpression, "1");

        var pagesProperty = clientClass.GetProperty("Pages");
        VerifyProperty(pagesProperty, SyntaxKind.NumericLiteralExpression, 100);

        var priceProperty = clientClass.GetProperty("Price");
        VerifyProperty(priceProperty, SyntaxKind.NumericLiteralExpression, 1.99);

        var titleProperty = clientClass.GetProperty("Title");
        VerifyProperty(titleProperty, SyntaxKind.StringLiteralExpression, "The Hobbit");

        var archiveProperty = clientClass.GetProperty("Archive");
        VerifyProperty(archiveProperty, SyntaxKind.TrueLiteralExpression, true);
    }

    [Fact]
    public async Task Interfaces()
    {
        var rawSchema = @"
            schema {
              query: Query
            }

            interface IFigure {
              id: Int
              perimeter: Float!
            }

            type Circle implements IFigure {
              id: Int
              center: Point!
              radius: Float!
              perimeter: Float!
            }

            type Point implements IFigure {
              id: Int!
              x: Float!
              y: Float!
              perimeter: Float!
            }

            type Square implements IFigure {
              id: Int
              topLeft: Point!
              bottomRight: Point!
              perimeter: Float!
            }

            type Query {
              figures: [IFigure!]!
              circles: [Circle!]!
              squares: [Square!]!
            }
        ";

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        var figureInterface = syntaxTree.GetInterface("IFigure")?.ToFullString();
        var squareClass = syntaxTree.GetClass("Square")?.ToFullString();
        var circleClass = syntaxTree.GetClass("Circle")?.ToFullString();
        var point = syntaxTree.GetClass("Point")?.ToFullString();
        var converter = syntaxTree.GetClass("ZeroQLIFigureConverter")?.ToFullString();
        var initializers = syntaxTree.GetClass("JsonConvertersInitializers")?.ToFullString();

        await Verify(new
        {
            figureInterface,
            squareClass,
            circleClass,
            point,
            converter,
            initializers
        });
    }

    [Fact]
    public async Task Union()
    {
        var rawSchema = @"
            schema {
              query: Query
            }

            type TextContent {
              text: String!
            }

            type ImageContent {
              imageUrl: String!
              height: Int!
            }

            union PostContent = TextContent | ImageContent

            type Query {
              posts: [PostContent!]!
            }
        ";

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        var postContentUnionInterface = syntaxTree.GetInterface("PostContent")?.ToFullString();
        var textContentClass = syntaxTree.GetClass("TextContent")?.ToFullString();
        var imageClass = syntaxTree.GetClass("ImageContent")?.ToFullString();
        var converter = syntaxTree.GetClass("ZeroQLPostContentConverter")?.ToFullString();
        var initializers = syntaxTree.GetClass("JsonConvertersInitializers")?.ToFullString();

        await Verify(new
        {
            postContentUnionInterface,
            textContentClass,
            imageClass,
            converter,
            initializers
        });
    }

    private void VerifyProperty(PropertyDeclarationSyntax property, SyntaxKind syntaxKind, object exprectedDefaultValue)
    {
        property.Should().NotBeNull();
        var initializer = property.Initializer;
        initializer.Should().NotBeNull();
        var bookIdDefaultValue = initializer!.Value as LiteralExpressionSyntax;
        bookIdDefaultValue.Should().NotBeNull();
        Assert.Equal(syntaxKind, bookIdDefaultValue!.Kind());
        Assert.Equal(exprectedDefaultValue, bookIdDefaultValue.Token.Value);
    }
}