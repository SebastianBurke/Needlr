namespace Needlr.Application.Common.Results;

/// <summary>
/// Outcome of a command or query. Use <see cref="Result{T}"/> when a successful outcome
/// carries a value; use <see cref="Result"/> when the only thing that matters is success/failure
/// (e.g., a status-transition command).
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>Convenience accessor when there is exactly one error (the common case).</summary>
    public Error? FirstError => Errors.Count > 0 ? Errors[0] : null;

    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
            throw new InvalidOperationException("A successful Result cannot carry errors.");
        if (!isSuccess && errors.Count == 0)
            throw new InvalidOperationException("A failed Result must carry at least one error.");

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, []);
    public static Result Failure(Error error) => new(false, [error]);
    public static Result Failure(IEnumerable<Error> errors) => new(false, errors.ToArray());
}

/// <summary>
/// Outcome of a command or query that produces a value on success.
/// </summary>
public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, [])
    {
        Value = value;
    }

    private Result(IReadOnlyList<Error> errors) : base(false, errors) { }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error error) => new([error]);
    public static new Result<T> Failure(IEnumerable<Error> errors) => new(errors.ToArray());

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
