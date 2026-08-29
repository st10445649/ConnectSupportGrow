namespace ConnectGrowAPI.Services;

public enum ErrorType
{
    None = 0,
    NotFound,
    Validation,
    Conflict,
    Forbidden,
    Unexpected
}

public class Result
{
    protected Result(bool isSuccess, ErrorType errorType, string? error)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public ErrorType ErrorType { get; }
    public string? Error { get; }

    public static Result Success() => new(true, ErrorType.None, null);
    public static Result Failure(ErrorType type, string error) => new(false, type, error);

    public static Result NotFound(string error) => Failure(ErrorType.NotFound, error);
    public static Result Invalid(string error) => Failure(ErrorType.Validation, error);
    public static Result Conflict(string error) => Failure(ErrorType.Conflict, error);
    public static Result Forbidden(string error) => Failure(ErrorType.Forbidden, error);
}

public sealed class Result<T> : Result
{
    private Result(T? value, bool isSuccess, ErrorType errorType, string? error)
        : base(isSuccess, errorType, error) => Value = value;

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, true, ErrorType.None, null);

    public static new Result<T> Failure(ErrorType type, string error) =>
        new(default, false, type, error);

    public static new Result<T> NotFound(string error) => Failure(ErrorType.NotFound, error);
    public static new Result<T> Invalid(string error) => Failure(ErrorType.Validation, error);
    public static new Result<T> Conflict(string error) => Failure(ErrorType.Conflict, error);
    public static new Result<T> Forbidden(string error) => Failure(ErrorType.Forbidden, error);
}