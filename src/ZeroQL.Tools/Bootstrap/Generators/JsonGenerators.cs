using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Extensions;
using ZeroQL.Schema;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ZeroQL.Bootstrap.Generators;

public static class JsonGenerators
{
    public static ClassDeclarationSyntax GenerateJsonInitializers(
        this GraphQlGeneratorOptions options,
        IReadOnlyList<ScalarDefinition> customScalars,
        IReadOnlyList<EnumDefinition> enums,
        IReadOnlyList<InterfaceDefinition> interfaces)
    {
        var customScalarInitializers = CustomScalarInitializers(options, customScalars);
        var enumInitializers = EnumInitializers(options, enums);
        var interfaceInitializers = InterfaceInitializers(options, interfaces);

        var source = @$"
            internal static class JsonConvertersInitializers
            {{
                [global::System.Runtime.CompilerServices.ModuleInitializer]
                public static void Init()
                {{
                    {customScalarInitializers}
                    {enumInitializers}
                    {interfaceInitializers}
                }} 
            }}
            ";

        var classDeclarationSyntax = ParseSyntaxTree(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        return classDeclarationSyntax;
    }

    private static StringBuilder InterfaceInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyList<InterfaceDefinition> interfaces)
    {
        var sb = new StringBuilder();
        foreach (var interfaceDefinition in interfaces)
        {
            var typeName = options.GetDefinitionFullTypeName(interfaceDefinition);
            sb.AppendLine(
                $"global::ZeroQL.Json.ZeroQLJsonSerializersStore.Converters[typeof({typeName})] = new ZeroQL{interfaceDefinition.Name}Converter();");
        }

        return sb;
    }

    private static StringBuilder CustomScalarInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyList<ScalarDefinition> customScalars)
    {
        var sb = new StringBuilder();
        foreach (var scalar in customScalars)
        {
            var fullTypeName = options.GetDefinitionFullTypeName(scalar);
            sb.AppendLine(
                $"global::ZeroQL.Json.ZeroQLJsonSerializersStore.Converters[typeof({fullTypeName})] = new ZeroQLScalarJsonConverter<{fullTypeName}>();");
        }

        return sb;
    }

    private static StringBuilder EnumInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyList<EnumDefinition> enums)
    {
        var enumInitializers = new StringBuilder();
        foreach (var @enum in enums)
        {
            var enumName = options.GetDefinitionFullTypeName(@enum);
            enumInitializers.AppendLine(
                @$"global::ZeroQL.Json.ZeroQLJsonSerializersStore.Converters[typeof({enumName})] =");
            enumInitializers.AppendLine(@$"
                new global::ZeroQL.Json.ZeroQLEnumConverter<{enumName}>(
                    new global::System.Collections.Generic.Dictionary<string, {enumName}>
                    {{");

            if (@enum.Values is not null)
            {
                foreach (var value in @enum.Values)
                {
                    enumInitializers.AppendLine(
                        @$"{{ ""{value}"", {enumName}.{value.ToPascalCase()} }}, ");
                }
            }

            enumInitializers.AppendLine(@$"
                }},
                new global::System.Collections.Generic.Dictionary<{enumName}, string>
                {{");

            if (@enum.Values is not null)
            {
                foreach (var value in @enum.Values)
                {
                    enumInitializers.AppendLine(
                        @$"{{ {enumName}.{value.ToPascalCase()}, ""{value}"" }},");
                }
            }

            enumInitializers.AppendLine("});");
        }

        return enumInitializers;
    }
}