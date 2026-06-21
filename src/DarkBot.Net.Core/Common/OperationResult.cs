namespace DarkBot.Net.Core.Common;

/// <summary>Универсальный результат операции.</summary>
public record OperationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }

    public static OperationResult Success() => new() { IsSuccess = true };

    public static OperationResult Failure(string errorMessage, Exception? exception = null) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, Exception = exception };
}

/// <summary>Результат операции с данными.</summary>
public record OperationResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }

    public static OperationResult<T> Success(T data) => new() { IsSuccess = true, Data = data };

    public static OperationResult<T> Failure(string errorMessage, Exception? exception = null) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, Exception = exception };
}
