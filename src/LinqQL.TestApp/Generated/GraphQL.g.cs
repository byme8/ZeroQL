using System.Text.Json.Serialization;

namespace GraphQL.TestServer
{
    public class KeyValuePairOfStringAndString
    {
        public string Key { get; set; }

        public string Value { get; set; }
    }

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

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Container")]
        public TypesContainer __Container { get; set; }

        public T Me<T>(Func<User, T> selector)
        {
            return selector(__Me);
        }

        public T[] Users<T>(UserFilterInput filter, int page, int size, Func<User, T> selector)
        {
            return __Users.Select(selector).ToArray();
        }

        public T User<T>(int id, Func<User, T> selector)
        {
            return selector(__User);
        }

        public T Admin<T>(int id, Func<User?, T> selector)
        {
            return selector(__Admin);
        }

        public T Container<T>(Func<TypesContainer, T> selector)
        {
            return selector(__Container);
        }
    }

    public class Role
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class TypesContainer
    {
        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value1")]
        public Byte __Value1 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value2")]
        public Byte? __Value2 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value13")]
        public Decimal __Value13 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value14")]
        public Decimal? __Value14 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value21")]
        public Guid[] __Value21 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value22")]
        public Guid[]? __Value22 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value23")]
        public Guid[] __Value23 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value24")]
        public Guid[]? __Value24 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value25")]
        public Guid[] __Value25 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value26")]
        public Guid[]? __Value26 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value27")]
        public KeyValuePairOfStringAndString[] __Value27 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value28")]
        public KeyValuePairOfStringAndString[]? __Value28 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value29")]
        public KeyValuePairOfStringAndString __Value29 { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Value30")]
        public KeyValuePairOfStringAndString? __Value30 { get; set; }

        public string Text { get; set; }

        public T Value1<T>(Func<Byte, T> selector)
        {
            return selector(__Value1);
        }

        public T Value2<T>(Func<Byte?, T> selector)
        {
            return selector(__Value2);
        }

        public short Value3 { get; set; }

        public short? Value4 { get; set; }

        public int Value5 { get; set; }

        public int? Value6 { get; set; }

        public long Value7 { get; set; }

        public long? Value8 { get; set; }

        public float Value9 { get; set; }

        public float? Value10 { get; set; }

        public float Value11 { get; set; }

        public float? Value12 { get; set; }

        public T Value13<T>(Func<Decimal, T> selector)
        {
            return selector(__Value13);
        }

        public T Value14<T>(Func<Decimal?, T> selector)
        {
            return selector(__Value14);
        }

        public DateTime Value15 { get; set; }

        public DateTime? Value16 { get; set; }

        public DateTime Value17 { get; set; }

        public DateTime? Value18 { get; set; }

        public Guid Value19 { get; set; }

        public Guid? Value20 { get; set; }

        public T[] Value21<T>(Func<Guid, T> selector)
        {
            return __Value21.Select(selector).ToArray();
        }

        public T[] Value22<T>(Func<Guid, T> selector)
        {
            return __Value22.Select(selector).ToArray();
        }

        public T[] Value23<T>(Func<Guid, T> selector)
        {
            return __Value23.Select(selector).ToArray();
        }

        public T[] Value24<T>(Func<Guid, T> selector)
        {
            return __Value24.Select(selector).ToArray();
        }

        public T[] Value25<T>(Func<Guid, T> selector)
        {
            return __Value25.Select(selector).ToArray();
        }

        public T[] Value26<T>(Func<Guid, T> selector)
        {
            return __Value26.Select(selector).ToArray();
        }

        public T[] Value27<T>(Func<KeyValuePairOfStringAndString, T> selector)
        {
            return __Value27.Select(selector).ToArray();
        }

        public T[] Value28<T>(Func<KeyValuePairOfStringAndString, T> selector)
        {
            return __Value28.Select(selector).ToArray();
        }

        public T Value29<T>(Func<KeyValuePairOfStringAndString, T> selector)
        {
            return selector(__Value29);
        }

        public T Value30<T>(Func<KeyValuePairOfStringAndString?, T> selector)
        {
            return selector(__Value30);
        }
    }

    public class User
    {
        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Role")]
        public Role __Role { get; set; }

        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public UserKind UserKind { get; set; }

        public T Role<T>(Func<Role, T> selector)
        {
            return selector(__Role);
        }
    }

    public class UserFilterInput
    {
        public UserKind UserKind { get; set; }
    }

    public enum UserKind
    {
        GOOD,
        BAD
    }
}