namespace RC.HyRe.Application.Common.Models;

public class Result
{
    internal Result(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors.ToArray();
    }

    public bool Succeeded { get; init; }

    public string[] Errors { get; init; }

    public static Result Success()
    {
        return new Result(true, Array.Empty<string>());
    }

    public static Result Failure(IEnumerable<string> errors)
    {
        return new Result(false, errors);
    }

    public static Result Failure(string error)
    {
        return new Result(false, [error]);
    }

    public static Result<T> Success<T>(T value)
    {
        return Result<T>.Success(value);
    }

    public static Result<T> Failure<T>(IEnumerable<string> errors)
    {
        return Result<T>.Failure(errors);
    }

    public static Result<T> Failure<T>(string error)
    {
        return Result<T>.Failure(error);
    }
}

public class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool succeeded, T? value, IEnumerable<string> errors) 
        : base(succeeded, errors)
    {
        _value = value;
    }

    public T Value => Succeeded && _value is not null
        ? _value
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, Array.Empty<string>());
    }

    public new static Result<T> Failure(IEnumerable<string> errors)
    {
        return new Result<T>(false, default, errors);
    }

    public new static Result<T> Failure(string error)
    {
        return new Result<T>(false, default, [error]);
    }
}
