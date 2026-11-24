namespace CloudTask.Web.Models;

public class ApiResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string? Error { get; init; }
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}