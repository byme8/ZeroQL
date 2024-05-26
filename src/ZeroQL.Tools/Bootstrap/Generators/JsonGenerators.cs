using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroQL.Extensions;
using ZeroQL.Schema;

namespace ZeroQL.Bootstrap.Generators;

public static class JsonGenerators
{
    public static string GenerateJsonInitializers(this GraphQlGeneratorOptions options,
        IReadOnlyCollection<ScalarDefinition> customScalars,
        IReadOnlyCollection<EnumDefinition> enums,
        IReadOnlyCollection<InterfaceDefinition> interfaces, 
        string[] typesForJsonContext)
    {
        var customScalarInitializers = CustomScalarInitializers(options, customScalars);
        var enumInitializers = EnumInitializers(options, enums);
        var interfaceInitializers = InterfaceInitializers(options, interfaces);

        var source = $$"""
                           internal static class {{options.JsonInitializerName}}
                           {
                               public static IZeroQLSerializer Configure(Action<JsonSerializerOptions> optionsFactory)
                               {
                                   var options = CreateOptions();
                                   optionsFactory(options);
                       
                                   return new ZeroQLSystemJsonSerializer(options);
                               }
                       
                               public static IZeroQLSerializer CreateSerializer(JsonSerializerOptions? options = null)
                               {
                                   return new ZeroQLSystemJsonSerializer(options ?? CreateOptions());
                               }
                       
                               public static JsonSerializerOptions CreateOptions()
                               {
                                   var options = ZeroQLJsonOptions.Create();
                                   options.TypeInfoResolver = new ZeroQLJsonSerializationContext();
                                   var converters = new Dictionary<Type, JsonConverter>
                                   {
                       {{customScalarInitializers.SpaceLeft(4)}}
                       {{enumInitializers.SpaceLeft(4)}}
                       {{interfaceInitializers.SpaceLeft(4)}}
                                   };
                       
                                   options.Converters.Insert(0, new ZeroQLConverter(converters));
                                   return options;
                               }
                              
                           }
                       
                           #if NET8_0
                       {{typesForJsonContext
                               .Select(o => $"[global::System.Text.Json.Serialization.JsonSerializable(typeof({o}))]")
                               .JoinWithNewLine()
                               .SpaceLeft(1)
                       }}
                           internal partial class ZeroQLJsonSerializationContext : JsonSerializerContext
                           {
                               
                           }
                           #endif
                       """;

        return source;
    }

    private static string InterfaceInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyCollection<InterfaceDefinition> interfaces)
    {
        var sb = new StringBuilder();
        foreach (var interfaceDefinition in interfaces)
        {
            var typeName = options.GetDefinitionFullTypeName(interfaceDefinition);
            sb.AppendLine($"{{ typeof({typeName}), new ZeroQL{interfaceDefinition.Name}Converter(options) }},");
        }

        return sb.ToString();
    }

    private static string CustomScalarInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyCollection<ScalarDefinition> customScalars)
    {
        var sb = new StringBuilder();
        foreach (var scalar in customScalars)
        {
            var fullTypeName = options.GetDefinitionFullTypeName(scalar);
            sb.AppendLine($"{{ typeof({fullTypeName}), new ZeroQLScalarJsonConverter<{fullTypeName}>() }},");
        }

        return sb.ToString();
    }

    private static string EnumInitializers(
        GraphQlGeneratorOptions options,
        IReadOnlyCollection<EnumDefinition> enums)
    {
        var enumInitializers = enums
            .Select(o =>
            {
                var enumName = options.GetDefinitionFullTypeName(o);
                var textToEnum = o.Values?
                    .Select(v => $$"""{ "{{v}}", {{enumName}}.{{v.ToPascalCase()}} },""")
                    .JoinWithNewLine()
                    .SpaceLeft(3);

                var enumToText = o.Values?
                    .Select(v => $$"""{ {{enumName}}.{{v.ToPascalCase()}}, "{{v}}" },""")
                    .JoinWithNewLine()
                    .SpaceLeft(3);

                return $$"""
                         {
                            typeof({{enumName}}),
                            new global::ZeroQL.Json.ZeroQLEnumConverter<{{enumName}}>(
                                new global::System.Collections.Generic.Dictionary<string, {{enumName}}>
                                {
                         {{textToEnum}}
                                },
                                new global::System.Collections.Generic.Dictionary<{{enumName}}, string>
                                {
                         {{enumToText}}
                                }
                            )
                          },
                         """;
            })
            .JoinWithNewLine();

        return enumInitializers;
    }
}