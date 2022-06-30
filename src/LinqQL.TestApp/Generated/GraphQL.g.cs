using System.Text.Json.Serialization;

namespace GraphQL.TestServer
{
    public class Query
    {
        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Me")]
        public User __Me { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Users")]
        public User[] __Users { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("User")]
        public User __User { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Admin")]
        public User? __Admin { get; set; }

        public T Me<T>(Func<User, T> selector)
        {
            return selector(__Me);
        }

        public T Users<T>(int page, int size, Func<User[], T> selector)
        {
            return selector(__Users);
        }

        public T User<T>(int id, Func<User, T> selector)
        {
            return selector(__User);
        }

        public T Admin<T>(int id, Func<User?, T> selector)
        {
            return selector(__Admin);
        }
    }

    public class Role
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class User
    {
        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Role")]
        public Role __Role { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public T Role<T>(Func<Role, T> selector)
        {
            return selector(__Role);
        }
    }
}