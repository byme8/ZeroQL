// This file generated for LinqQL.
// <auto-generated/>
using System; 
using System.Linq; 
using System.Text.Json.Serialization; 

namespace GraphQL.TestServer
{
    [System.CodeDom.Compiler.GeneratedCode ( "LinqQL" ,  "1.0.0.0" )]
    public class KeyValuePairOfStringAndString
    {
        public string Key { get; set; }

        public string Value { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCode ( "LinqQL" ,  "1.0.0.0" )]
    public class TestServerQuery
    {
        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Me")]
        public User __Me { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("Users")]
        public User[] __Users { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("UserKinds")]
        public UserKind[] __UserKinds { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("UsersByKind")]
        public User[] __UsersByKind { get; set; }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never), JsonPropertyName("UsersIds")]
        public int[] __UsersIds { get; set; }

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

        public UserKind[] UserKinds()
        {
            return __UserKinds;
        }

        public T[] UsersByKind<T>(UserKind kind, int page, int size, Func<User, T> selector)
        {
            return __UsersByKind.Select(selector).ToArray();
        }

        public int[] UsersIds(UserKind kind, int page, int size)
        {
            return __UsersIds;
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

    [System.CodeDom.Compiler.GeneratedCode ( "LinqQL" ,  "1.0.0.0" )]
    public class Role
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCode ( "LinqQL" ,  "1.0.0.0" )]
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

        public Guid[] Value21()
        {
            return __Value21;
        }

        public Guid[] Value22()
        {
            return __Value22;
        }

        public Guid[] Value23()
        {
            return __Value23;
        }

        public Guid[] Value24()
        {
            return __Value24;
        }

        public Guid[] Value25()
        {
            return __Value25;
        }

        public Guid[] Value26()
        {
            return __Value26;
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

    [System.CodeDom.Compiler.GeneratedCode ( "LinqQL" ,  "1.0.0.0" )]
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

    [System.CodeDom.Compiler.GeneratedCode ( "LinqQL" ,  "1.0.0.0" )]
    public class UserFilterInput
    {
        public UserKind UserKind { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCode ( "LinqQL" ,  "1.0.0.0" )]
    public enum UserKind
    {
        GOOD,
        BAD
    }
}