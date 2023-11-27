using System.Collections.Generic;
using System.Text;
using ZeroQL.Extensions;
using ZeroQL.Schema;

namespace ZeroQL.Bootstrap.Generators;

public static class JsonGenerators
{
    public static string GenerateJsonInitializers(
        this GraphQlGeneratorOptions options,
        string? queryType,
        string? mutationType,
        IReadOnlyCollection<ScalarDefinition> customScalars,
        IReadOnlyCollection<EnumDefinition> enums,
        IReadOnlyCollection<InterfaceDefinition> interfaces)
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
            #if NET8_0
                    global::ZeroQL.Json.ZeroQLJsonOptions.AddJsonContext(new ZeroQLJsonSerializationContext());
            #endif 
                    {customScalarInitializers}
                    {enumInitializers}
                    {interfaceInitializers}
                }} 
            }}

            #if NET8_0
            {(!string.IsNullOrEmpty(queryType)
                ? $"[global::System.Text.Json.Serialization.JsonSerializable(typeof({queryType}))]"
                : string.Empty)}
            {(!string.IsNullOrEmpty(mutationType)
                ? $"[global::System.Text.Json.Serialization.JsonSerializable(typeof({mutationType}))]"
                : string.Empty)}
            internal partial class ZeroQLJsonSerializationContext : JsonSerializerContext
            {{
                
            }}
            #endif
        ";

        return source;
    }

    private static StringBuilder InterfaceInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyCollection<InterfaceDefinition> interfaces)
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
        IReadOnlyCollection<ScalarDefinition> customScalars)
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
        IReadOnlyCollection<EnumDefinition> enums)
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