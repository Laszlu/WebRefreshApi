using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiBase.CustomRequestResult;

public enum ApiActionResult
{
    Success = 0,
    NotFound = 1,
    AlreadyDeleted = 2,
    InvalidRequest = 3,
    InternalError = 4
}

public sealed record ApiResult(ApiActionResult Result, string? ErrorMessage = null)
{
    public static ApiResult Success() => new(ApiActionResult.Success);
    public static ApiResult Fail(ApiActionResult result, string? message = null)
        => new(result, message);
}

public sealed record ApiResultResultWithPayload<T>(ApiActionResult Result, T? Value = default, string? ErrorMessage = null)
{
    public static ApiResultResultWithPayload<T> Success(T value) => new(ApiActionResult.Success, value);
    public static ApiResultResultWithPayload<T> Fail(
        ApiActionResult result, string? message = null, T? value = default)
        => new(result, value, message);
}

public static class ApiResultExtensions
{
    private static int ToStatusCode(this ApiActionResult result) => result switch
    {
        ApiActionResult.Success => StatusCodes.Status200OK,

        ApiActionResult.NotFound => StatusCodes.Status404NotFound,
        
        ApiActionResult.AlreadyDeleted => StatusCodes.Status410Gone,
        
        ApiActionResult.InvalidRequest => StatusCodes.Status400BadRequest,

        ApiActionResult.InternalError => StatusCodes.Status500InternalServerError,
        
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
    };
    
    public static IActionResult ToActionResult(this ApiResult result)
    {
        var status = result.Result.ToStatusCode();
        return status == StatusCodes.Status200OK
            ? new OkResult()
            : new ObjectResult(new { message = result.ErrorMessage }) { StatusCode = status };
    }

    public static IActionResult ToActionResult<T>(this  ApiResultResultWithPayload<T> resultWithPayload)
    {
        var status = resultWithPayload.Result.ToStatusCode();
        var isSuccess = status is >= 200 and < 300;

        object? body = isSuccess
            ? resultWithPayload.Value
            : new { message = resultWithPayload.ErrorMessage };

        return new ObjectResult(body) { StatusCode = status };
    }
}