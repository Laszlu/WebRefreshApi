using Microsoft.AspNetCore.Authentication;

namespace ApiBase.AuthenticationAuthorization.Options;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string HeaderPrefix = "x-api-key ";
    
    public bool SendInvalidKeyMail { get; set; } = true;
    public TimeSpan MailThrottleInterval { get; set; } = TimeSpan.FromSeconds(0);
}