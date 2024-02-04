using System.Text.Json;

namespace ZeroQL.TestServer.Query;

[QueryType]
public class JSONQueryExtensions
{
    public JsonElement GetJsonUsersElement() => JsonSerializer.SerializeToElement(UserGraphQLExtensions.Users.Values.ToArray());

    public JsonDocument GetJsonUsersDocument() => JsonSerializer.SerializeToDocument(UserGraphQLExtensions.Users.Values.ToArray());
}