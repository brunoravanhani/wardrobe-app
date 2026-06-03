namespace VirtualWardrobe.Application.Common;

public sealed record ResultError(string Code, string Message)
{
    public static readonly ResultError None = new(string.Empty, string.Empty);

    public static ResultError Validation(string message) => new("validation_error", message);
    public static ResultError NotFound(string message) => new("not_found", message);
    public static ResultError Forbidden(string message) => new("forbidden", message);
}

public class Result
{
    protected Result(bool isSuccess, ResultError error)
    {
        if (isSuccess && error != ResultError.None)
        {
            throw new InvalidOperationException("Successful result cannot have an error.");
        }

        if (!isSuccess && error == ResultError.None)
        {
            throw new InvalidOperationException("Failure result must have an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ResultError Error { get; }

    public static Result Success() => new(true, ResultError.None);

    public static Result Failure(ResultError error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.CreateSuccess(value);

    public static Result<T> Failure<T>(ResultError error) => Result<T>.CreateFailure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, ResultError.None)
    {
        _value = value;
    }

    private Result(ResultError error)
        : base(false, error)
    {
        _value = default;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Failure result does not contain a value.");

    internal static Result<T> CreateSuccess(T value) => new(value);

    internal static Result<T> CreateFailure(ResultError error) => new(error);
}
