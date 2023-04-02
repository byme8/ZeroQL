using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Schema;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZeroQL.Bootstrap.Generators;

public static class ScalarGenerator
{
    public static RecordDeclarationSyntax[] GenerateScalars(this GraphQlGeneratorOptions options,
        IReadOnlyList<ScalarDefinition> customScalars)
    {
        return customScalars
            .Select(scalarDefinition =>
            {
                var source = $$"""
                    {{options.AccessLevel}} sealed record {{scalarDefinition.Name}} : ZeroQLScalar
                    {
                        public {{scalarDefinition.Name}}()
                        {
                        }

                        public {{scalarDefinition.Name}}(string value)
                        {
                            Value = value;
                        }

                        public static implicit operator {{scalarDefinition.Name}}(string value) => new {{scalarDefinition.Name}}(value);

                        public static implicit operator string({{scalarDefinition.Name}} scalar) => scalar.Value;
                    }
                    """;

                var classDeclarationSyntax = ParseCompilationUnit(source)
                    .DescendantNodesAndSelf()
                    .OfType<RecordDeclarationSyntax>()
                    .First();

                return classDeclarationSyntax;
            })
            .ToArray();
    }
}