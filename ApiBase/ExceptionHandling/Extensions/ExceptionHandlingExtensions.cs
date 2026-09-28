using ApiBase.ExceptionHandling.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace ApiBase.ExceptionHandling.Extensions;

public static class ExceptionHandlingExtensions
{
    public static IServiceCollection AddBaseExceptionHandler(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}