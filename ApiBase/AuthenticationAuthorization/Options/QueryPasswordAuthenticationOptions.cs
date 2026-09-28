using Microsoft.AspNetCore.Authentication;

namespace ApiBase.AuthenticationAuthorization.Options;

public class QueryPasswordAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "QueryPassword";
    public string QueryParameterName { get; set; } = "password";

    public bool SendInvalidKeyMail { get; set; } = true;
    public TimeSpan InvalidKeyMailInterval { get; set; } = TimeSpan.FromSeconds(0);
}