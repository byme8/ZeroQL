using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace System;

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

    public TValue? Value { get; }

    public Error? Error { get; }

    public static implicit operator Result<TValue>(TValue value) => new(value);

    public static implicit operator Result<TValue>(Error error) => new(error);
}
public class Error
{
    public Error(string message)
    {
        Message = message;
    }

    public string Message { get; }

    public static implicit operator bool(Error? error) => error is not null;
}

public class ErrorWithData<TData> : Error
{
    public ErrorWithData(string message, TData data)
        : base(message)
    {
        Data = data;
    }

    public TData Data { get; }
}

public static class ResultExtensions
{
    public static (TValue Value, Error error) Unwrap<TValue>(this Result<TValue> result)
    {
        if (result.Value is not null)
        {
            return (result.Value, null!);
        }
        
        return (default, result.Error)!;
    }
    
    public static async Task<(TValue Value, Error error)> Unwrap<TValue>(this Task<Result<TValue>> task)
    {
        var result = await task;
        if (result.Value is not null)
        {
            return (result.Value, null!);
        }
        
        return (default, result.Error)!;
    }
}