namespace LinqQL.Core
{
    public class SourceGeneratorInfo
    {
        public static string Version { get; } = typeof(SourceGeneratorInfo).Assembly.GetName().Version.ToString();
        public static string CodeGenerationAttribute { get; } = $@"System.CodeDom.Compiler.GeneratedCode(""LinqQL"", ""{Version}"")";
    }
}