using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Extensions;
using ZeroQL.Internal;
using ZeroQL.Schema;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZeroQL.Bootstrap.Generators;

public static class InterfaceGenerator
{
    public static IEnumerable<MemberDeclarationSyntax> GenerateInterfaces(
        this GraphQlGeneratorOptions options,
        IReadOnlyCollection<InterfaceDefinition> interfaces)
    {
        var csharpDefinitions = interfaces
            .SelectMany(o =>
            {
                var fields = o.Properties
                    .SelectMany(TypeGenerator.GeneratePropertiesDeclarations)
                    .ToArray();

                var @interface = CSharpHelper.Interface(o.Name, options.Visibility)
                    .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                    .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("global::ZeroQL.IUnionType")))
                    .WithMembers(SyntaxFactory.List(fields));

                var stub = CSharpHelper.Class(o.Name + "Stub", options.Visibility)
                    .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                    .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(o.Name)))
                    .WithMembers(SyntaxFactory.List(fields));

                return new MemberDeclarationSyntax[] { @interface, stub };
            })
            .ToList();

        return csharpDefinitions;
    }
    
    public static ClassDeclarationSyntax[] GenerateInterfaceInitializers(
        this List<InterfaceDefinition> interfaces,
        ClassDefinition[] types)
    {
        var typesByInterface = types
            .Where(o => o.Implements.Any())
            .SelectMany(o => o.Implements.Select(oo => (Interface: oo, Type: o)))
            .GroupBy(o => o.Interface)
            .ToDictionary(o => o.Key, o => o.ToArray());

        foreach (var @interface in interfaces)
        {
            if (typesByInterface.ContainsKey(@interface.Name))
            {
                continue;
            }

            typesByInterface.Add(@interface.Name, Array.Empty<(string Interface, ClassDefinition Type)>());
        }

        if (!typesByInterface.Any())
        {
            return Array.Empty<ClassDeclarationSyntax>();
        }

        var classes = typesByInterface.Select(group =>
            {
                var typeName = group.Key;
                var source = $$"""
                    internal class ZeroQL{{typeName}}Converter : InterfaceJsonConverter<{{typeName}}?>
                    {
                        public override {{typeName}}? Deserialize(string typeName, JsonObject json) =>
                            typeName switch
                            {
                                {{group.Value
                                    .Select(o => $@"""{o.Type.Name}"" => json.Deserialize<{o.Type.Name}>(ZeroQLJsonOptions.Options),")
                                    .JoinWithNewLine()}}
                                _ => json.Deserialize<{{typeName}}Stub>(ZeroQLJsonOptions.Options)
                            };
                    }

                    """;

                return source;
            })
            .JoinWithNewLine();

        var syntaxTree = ParseSyntaxTree(classes)
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .ToArray();

        return syntaxTree;
    }

}