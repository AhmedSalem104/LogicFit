namespace LogicFit.Application.Common.Models;

public class Result
{
    internal Result(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors.ToArray();
    }

    public bool Succeeded { get; }
    public string[] Errors { get; }

    public static Result Success() => new(true, Array.Empty<string>());

    public static Result Failure(IEnumerable<string> errors) => new(false, errors);

    public static Result Failure(string error) => new(false, new[] { error });
}

public class Result<T> : Result
{
    internal Result(T? data, bool succeeded, IEnumerable<string> errors)
        : base(succeeded, errors)
    {
        Data = data;
    }

    public T? Data { get; }

    public static Result<T> Success(T data) => new(data, true, Array.Empty<string>());

    public static new Result<T> Failure(IEnumerable<string> errors) => new(default, false, errors);

    public static new Result<T> Failure(string error) => new(default, false, new[] { error });
}
