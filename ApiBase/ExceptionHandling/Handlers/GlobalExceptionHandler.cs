using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace ApiBase.ExceptionHandling.Handlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var endpoint = context.GetEndpoint();
        var route = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>()?.AttributeRouteInfo?.Template
                    ?? (endpoint as RouteEndpoint)?.RoutePattern.RawText
                    ?? endpoint?.DisplayName;

        _logger.LogError(exception, "An error occurred in {Route} (path: {Path}). ExceptionType {ExceptionType}.", route, context.Request.Path, exception.GetType());

        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Exception after response started for {Path}", context.Request.Path);
            return false;
        }
        
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Unhandled exception occurred. View error logs on API host."
        }, cancellationToken);

        return true;
    }
}