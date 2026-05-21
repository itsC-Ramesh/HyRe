namespace RC.HyRe.Web.Infrastructure;

public class ApiResponse
{
    public bool Success { get; init; }
    public ApiError? Error { get; init; }

    public static ApiResponse Ok()
    {
        return new ApiResponse { Success = true };
    }

    public static ApiResponse<T> Ok<T>(T data)
    {
        return ApiResponse<T>.Ok(data);
    }
    
    public static ApiResponse Fail(string code, string message, object? details = null)
    {
        return new ApiResponse
        {
            Success = false,
            Error = new ApiError(code, message, details)
        };
    }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }
}

public record ApiError(string Code, string Message, object? Details);
