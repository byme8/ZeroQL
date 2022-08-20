namespace ZeroQL.Core;

public class ZeroQLGenerationInfo
{
    public static string Version { get; } = typeof(ZeroQLGenerationInfo).Assembly.GetName().Version!.ToString();
    
    public static string CodeGenerationAttribute { get; } = $@"System.CodeDom.Compiler.GeneratedCode(""ZeroQL"", ""{Version}"")";
    
    public static string GraphQLFieldSelectorAttribute { get; } = $@"ZeroQL.Core.GraphQLFieldSelector";
}