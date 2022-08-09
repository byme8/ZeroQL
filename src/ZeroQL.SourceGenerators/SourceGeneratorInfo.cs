namespace ZeroQL.SourceGenerators;

public class SourceGeneratorInfo
{
    public static string Version { get; } = typeof(SourceGeneratorInfo).Assembly.GetName().Version.ToString();

    public static string CodeGenerationAttribute { get; } = $@"[System.CodeDom.Compiler.GeneratedCode(""ZeroQL"", ""{Version}"")]";

    public static string GraphQLFieldSelectorAttribute { get; } = "ZeroQL.Core.GraphQLFieldSelector";

    public static string GraphQLFragmentAttributeFullName { get; } = "ZeroQL.Core.GraphQLFragment";
    
    public static string GraphQLFragmentAttributeTypeName { get; } = "GraphQLFragment";

    public static string GraphQLQueryTemplateAttribute { get; } = "ZeroQL.Core.GraphQLQueryTemplate";
}