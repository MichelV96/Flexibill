namespace MVSoftware.Flexibill.Application.Common;

/// <summary>
/// Commands/queries retourneren dit in plaats van exceptions te gooien voor verwachte
/// fouten (Technisch Ontwerp, hoofdstuk 6.1) - de UI-laag kan zo nette foutmeldingen
/// tonen zonder overal try/catch nodig te hebben.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? error) : base(isSuccess, error) => Value = value;

    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(string error) => new(false, default, error);
}
