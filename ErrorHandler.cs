public enum ErrorType { Validation, Unauthorized, Forbidden, NotFound, Conflict, BadRequest }

public class ServiceResult<T>
{
    public bool Success { get; private init; }
    public T? Data { get; private init; }
    public string? Error { get; private init; }
    public ErrorType? ErrorType { get; private init; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };

    public static ServiceResult<T> NoContent() => new() { Success = true };

    public static ServiceResult<T> Fail(ErrorType type, string error) =>
        new() { Success = false, ErrorType = type, Error = error };

    public static ServiceResult<T> Errored(ErrorType type, T data) =>
        new() { Success = false, ErrorType = type, Data = data };
}
