using System.Text.Json.Serialization;

namespace GraphQL.TestServer
{
    public class Query
    {
        [JsonPropertyName("Me")]
        User _Me;
        [JsonPropertyName("Users")]
        User[] _Users;
        [JsonPropertyName("User")]
        User _User;
        [JsonPropertyName("Admin")]
        User? _Admin;
        public T Me<T>(Func<User, T> selector)
        {
            return selector(_Me);
        }

        public T Users<T>(int page, int size, Func<User[], T> selector)
        {
            return selector(_Users);
        }

        public T User<T>(int id, Func<User, T> selector)
        {
            return selector(_User);
        }

        public T Admin<T>(int id, Func<User?, T> selector)
        {
            return selector(_Admin);
        }
    }

    public class Role
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class User
    {
        [JsonPropertyName("Role")]
        Role _Role;
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public T Role<T>(Func<Role, T> selector)
        {
            return selector(_Role);
        }
    }
}