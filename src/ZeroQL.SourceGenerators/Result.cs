namespace ZeroQL.SourceGenerators;

public class Result<TValue>
{
    private Result(TValue value)
    {
        Value = value;
    }

    private Result(Error error)
    {
        Error = error;   
    }
    
    public TValue Value { get; }

    public Error Error { get; }

    public static implicit operator Result<TValue>(TValue value) => new(value);

    public static implicit operator Result<TValue>(Error error) => new(error);
}

public class Error
{
    public Error(string code)
    {
        Code = code;
    }

    public string Code { get; }
    
    public static implicit operator bool(Error? error) => error is not null;
}

public class ErrorWithData<TData> : Error
{
    public ErrorWithData(string code, TData data) 
        : base(code)
    {
        Data = data;
    }
    
    public TData Data { get; }
}
