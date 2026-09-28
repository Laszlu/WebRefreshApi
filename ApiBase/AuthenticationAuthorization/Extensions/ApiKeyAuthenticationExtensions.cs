using ApiBase.AuthenticationAuthorization.Handler;
using ApiBase.AuthenticationAuthorization.Helper;
using ApiBase.AuthenticationAuthorization.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiBase.AuthenticationAuthorization.Extensions;

public static class ApiKeyAuthenticationExtensions
{
    public static AuthenticationBuilder AddApiKeyAuthentication(this AuthenticationBuilder builder, Action<ApiKeyAuthenticationOptions>? configure = null)
    {
        builder.Services.TryAddSingleton<DevMailThrottleHelper>();
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.DefaultScheme, configure ?? (_ => { }));
    }
    
    public static AuthenticationBuilder AddQueryPasswordAuthentication(this AuthenticationBuilder builder, Action<QueryPasswordAuthenticationOptions>? configure = null)
    {
        builder.Services.TryAddSingleton<DevMailThrottleHelper>();
        return builder.AddScheme<QueryPasswordAuthenticationOptions, QueryPasswordAuthenticationHandler>(QueryPasswordAuthenticationOptions.DefaultScheme, configure ?? (_ => { }));
    }
}