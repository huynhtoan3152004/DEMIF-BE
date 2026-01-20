namespace Demif.Api.Contracts;

/// <summary>
/// Response wrapper chuẩn cho API
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static ApiResponse<T> Fail(IEnumerable<string> errors) => new()
    {
        Success = false,
        Errors = errors
    };
}
