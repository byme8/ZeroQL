using System.Text.Json;

namespace ZeroQL.TestServer.Query;

[ExtendObjectType(typeof(Query))]
public class JSONQueryExtensions
{
    public JsonElement GetJsonUsersElement() => JsonSerializer.SerializeToElement(UserGraphQLExtensions.Users.Values.ToArray());

    public JsonDocument GetJsonUsersDocument() => JsonSerializer.SerializeToDocument(UserGraphQLExtensions.Users.Values.ToArray());
}