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
        IReadOnlyDictionary<string, InterfaceDefinition> interfaces)
    {
        var containsNodeInterface = interfaces.ContainsKey("Node");
        var csharpDefinitions = interfaces.Values
            .SelectMany(o =>
            {
                var interfaceFields = o.Properties
                    .SelectMany(p => p.GeneratePropertiesDeclarations(true))
                    .ToArray();

                var containsIdProperty = o.Name != "Node" &&
                                         o.Properties.Any(p => p is { Name: "Id", TypeDefinition.Name: "ID" });

                var stubFields = o.Properties
                    .SelectMany(p => p.GeneratePropertiesDeclarations())
                    .ToArray();

                var baseTypes = o.Implemented
                    .Select(o => SimpleBaseType(ParseTypeName(o)))
                    .ToList();

                baseTypes.Add(SimpleBaseType(ParseTypeName("global::ZeroQL.IUnionType")));
                if (containsNodeInterface && containsIdProperty)
                {
                    baseTypes.Add(SimpleBaseType(ParseTypeName("Node")));
                }

                var @interface = CSharpHelper.Interface(o.Name, options.Visibility)
                    .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                    .AddBaseListTypes(baseTypes.ToArray())
                    .WithMembers(List(interfaceFields));

                var stub = CSharpHelper.Class(o.Name + "Stub", options.Visibility)
                    .AddAttributes(ZeroQLGenerationInfo.CodeGenerationAttribute)
                    .AddBaseListTypes(SimpleBaseType(ParseTypeName(o.Name)))
                    .WithMembers(List(stubFields));

                return new MemberDeclarationSyntax[] { @interface, stub };
            })
            .ToList();

        return csharpDefinitions;
    }

    public static ClassDeclarationSyntax[] GenerateInterfaceInitializers(
        this Dictionary<string, InterfaceDefinition> interfaces,
        ClassDefinition[] types)
    {
        var typesByInterface = types
            .Where(o => o.Implements.Any())
            .SelectMany(o => o.Implements.Select(oo => (Interface: oo.Name, Type: o)))
            .GroupBy(o => o.Interface)
            .ToDictionary(o => o.Key, o => o.ToArray());

        foreach (var @interface in interfaces.Values.Select(o => o.Name))
        {
            if (typesByInterface.ContainsKey(@interface))
            {
                continue;
            }

            typesByInterface.Add(@interface, Array.Empty<(string Interface, ClassDefinition Type)>());
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