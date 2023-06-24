namespace ZeroQL.SourceGenerators;

public class SourceGeneratorInfo
{
    public static string Version { get; } = typeof(SourceGeneratorInfo).Assembly.GetName().Version.ToString();

    public static string CodeGenerationAttribute { get; } = $@"[System.CodeDom.Compiler.GeneratedCode(""ZeroQL"", ""{Version}"")]";

    public static string GraphQLUnionType { get; } = "ZeroQL.IUnionType";

    public static string GraphQLSyntaxAttribute { get; } = "ZeroQL.GraphQLSyntax";

    public static string GraphQLNameAttribute { get; } = "ZeroQL.GraphQLNameAttribute";

    public static string GraphQLFragmentAttributeFullName { get; } = "ZeroQL.GraphQLFragment";
    
    public static string GraphQLFragmentAttributeTypeName { get; } = "GraphQLFragment";

    public static string GraphQLQueryTemplateAttribute { get; } = "ZeroQL.GraphQLQueryTemplate";
}