namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(Query))]
public class CSharpKeywordsQueryExtensions
{
    public int GetInt(int @int) => @int;

    public int GetObject(int @object) => @object;
}