using Microsoft.AspNetCore.Mvc;

namespace WebRefreshSiteBuilderAPI.Data;

public struct VersionInfo
{
    public string CommitHash { get; set; }
    
    public string BuildDate { get; set; }
}

public enum SiteBuilderActionResult
{
    Success = 0,
    NotFound = 1,
    AgentError = 2,
    HtmlParseError = 3
}

public sealed record SiteBuilderApiResult(SiteBuilderActionResult ActionResult, string? ErrorMessage = null)
{
    public static SiteBuilderApiResult Success() => new(SiteBuilderActionResult.Success);
    
    public static SiteBuilderApiResult Fail(SiteBuilderActionResult result, string? errorMessage = null) 
        => new(result, errorMessage);
}

public sealed record SiteBuilderApiResultWithPayload<T>(
    SiteBuilderActionResult ActionResult,
    T? Payload = default,
    string? ErrorMessage = null)
{
    public static SiteBuilderApiResultWithPayload<T> Success(T payload) => new(SiteBuilderActionResult.Success, payload);
    
    public static SiteBuilderApiResultWithPayload<T> Fail(SiteBuilderActionResult result, T? value = default, string? errorMessage = null) 
        => new(result, value, errorMessage);
}

public static class SiteBuilderApiResultExtensions
{
    private static int ToStatusCode(this SiteBuilderActionResult actionResult) => actionResult switch
    {
        SiteBuilderActionResult.Success => StatusCodes.Status200OK,
        
        SiteBuilderActionResult.NotFound => StatusCodes.Status404NotFound,
        
        SiteBuilderActionResult.AgentError 
            or SiteBuilderActionResult.HtmlParseError 
            or _ => StatusCodes.Status418ImATeapot
    };

    public static IActionResult ToActionResult(this SiteBuilderApiResult actionResult)
    {
        var status = actionResult.ActionResult.ToStatusCode();
        return status == StatusCodes.Status200OK
            ? new OkResult()
            : new ObjectResult(new { message = actionResult.ErrorMessage }) {StatusCode = status};
    }
    
    public static IActionResult ToActionResult<T>(this SiteBuilderApiResultWithPayload<T> actionResultWithPayload)
    {
        var status = actionResultWithPayload.ActionResult.ToStatusCode();
        var isSuccess = status is >= 200 and < 300;

        object? body = isSuccess
            ? actionResultWithPayload.Payload
            : new { message = actionResultWithPayload.ErrorMessage };

        return new ObjectResult(body) { StatusCode = status };
    }
}