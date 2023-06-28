using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Bootstrap;
using ZeroQL.Extensions;

namespace ZeroQL.Tests.Bootstrap;

[UsesVerify]
public class CustomSchemaParseTests
{
    [Fact]
    public void DefaultValuesForInputObjects()
    {
        const string clientClassName = "Book";
        var rawSchema = $$"""
            schema {
              mutation: Mutation
            }

            input {{clientClassName}} {
              bookId: ID! = 1
              pages: int! = 100
              price: float! = 1.99
              title: string = "The Hobbit"
              archive: bool = true
              
            }

            type Mutation {
              addBook(input: Book): int
            }
            """;

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
        var rawSchema = """
            schema {
              query: Query
            }

            interface IUser {
              id: ID!
              name: String!
            }

            type User implements IUser {
              id: ID!
              name: String!
            }

            interface IFigure {
              id: Int
              perimeter: Float!
              creator: IUser!
            }

            type Circle implements IFigure {
              id: Int
              center: Point!
              radius: Float!
              perimeter: Float!
              creator: User!
            }

            type Point implements IFigure {
              id: Int!
              x: Float!
              y: Float!
              perimeter: Float!
              creator: IUser!
            }

            type Square implements IFigure {
              id: Int
              topLeft: Point!
              bottomRight: Point!
              perimeter: Float!
              creator: IUser!
            }

            type Query {
              figures: [IFigure!]!
              circles: [Circle!]!
              squares: [Square!]!
            }     
            """;

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        var figureInterface = syntaxTree.GetInterface("IFigure")?.ToFullString();
        var figureStubInterface = syntaxTree.GetClass("IFigureStub")?.ToFullString();
        var squareClass = syntaxTree.GetClass("Square")?.ToFullString();
        var circleClass = syntaxTree.GetClass("Circle")?.ToFullString();
        var point = syntaxTree.GetClass("Point")?.ToFullString();
        var converter = syntaxTree.GetClass("ZeroQLIFigureConverter")?.ToFullString();
        var initializers = syntaxTree.GetClass("JsonConvertersInitializers")?.ToFullString();

        await Verify(new
        {
            figureInterface,
            figureStubInterface,
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

    [Fact]
    public async Task ClassNameIdenticalToPropertyName()
    {
        var rawSchema = @"
            schema {
              query: Query
            }
              
            type Query {
              perVariant: Limit!
            }

            type Limit {
              limit: Long
            }
        ";

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        var clases = (await syntaxTree.GetRootAsync())
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(o => o.Identifier.ValueText.Contains("Limit"))
            .Select(o => o.Identifier.ValueText)
            .ToArray();

        await Verify(clases);
    }

    [Fact]
    public async Task InterfaceNameIdenticalToPropertyName()
    {
        var rawSchema = @"
            schema {
              query: Query
            }
              
            type Query {
              perVariant: Limit!
            }

            interface Limit {
              limit: Long
            }
        ";

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        var clases = (await syntaxTree.GetRootAsync())
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(o => o.Identifier.ValueText.Contains("Limit"))
            .Select(o => o.Identifier.ValueText)
            .ToArray();

        await Verify(clases);
    }

    [Fact]
    public async Task InputTypeNameIdenticalToPropertyName()
    {
        var rawSchema = @"
            schema {
              mutation: Mutation
            }
              
            type Mutation {
              ids(limit: Limit!) : [Long!]!
            }

            input Limit {
              limit: Long
            }
        ";

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        var clases = (await syntaxTree.GetRootAsync())
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(o => o.Identifier.ValueText.Contains("Limit"))
            .Select(o => o.Identifier.ValueText)
            .ToArray();

        await Verify(clases);
    }

    [Fact]
    public async Task SchemaCanBeOmitted()
    {
        var rawSchema = @"
            type Mutation {
              addUser(input: AddUserInput!) : User!
            }

            type Query {
              user(id: Long!) : User!
            }

            type User {
              id: Long!
              name: String!
            }

            input AddUserInput {
              name: String!
            }
        ";

        var csharp = GraphQLGenerator.ToCSharp(rawSchema, "TestApp", "GraphQLClient");
        var syntaxTree = CSharpSyntaxTree.ParseText(csharp);

        var client = syntaxTree.GetClass("GraphQLClient")!;
        var query = syntaxTree.GetClass("Query")!;
        var mutation = syntaxTree.GetClass("Mutation")!;


        await Verify(new
        {
            client = client.Identifier.ToFullString() + client.BaseList!.ToFullString(),
            query = query.Identifier.ToFullString() + query.BaseList!.ToFullString(),
            mutation = mutation.Identifier.ToFullString() + mutation.BaseList!.ToFullString(),
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