using System.Diagnostics.CodeAnalysis;

namespace Holmes.Core.Domain.Results;

public class Result
{
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public string? Error { get; }

    public static Result Success()
    {
        return new Result(true, null);
    }

    public static Result Fail(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Result(false, error);
    }

    public static Result<T> Success<T>(T value)
    {
        return Result<T>.Success(value);
    }

    public static Result<T> Fail<T>(string error)
    {
        return Result<T>.Fail(error);
    }
}

public class Result<T> : Result
{
    private readonly T? _value;

    protected Result(bool isSuccess, T? value, string? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null);
    }

    public new static Result<T> Fail(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Result<T>(false, default, error);
    }

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value!;
        return IsSuccess;
    }
}