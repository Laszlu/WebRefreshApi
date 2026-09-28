using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using ApiBase.AuthenticationAuthorization.Helper;
using ApiBase.AuthenticationAuthorization.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiBase.AuthenticationAuthorization.Handler;

public class QueryPasswordAuthenticationHandler : AuthenticationHandler<QueryPasswordAuthenticationOptions>
{
    private readonly IConfiguration _config;
    private readonly DevMailThrottleHelper _throttleHelper;
    
    public QueryPasswordAuthenticationHandler(IOptionsMonitor<QueryPasswordAuthenticationOptions> options, ILoggerFactory loggerFactory, UrlEncoder encoder, IConfiguration config, DevMailThrottleHelper throttleHelper) : base(options, loggerFactory, encoder)
    {
        _config = config;
        _throttleHelper = throttleHelper;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var provided = Request.Query[Options.QueryParameterName].ToString();
        
        if (string.IsNullOrWhiteSpace(provided))
        {
            return Task.FromResult(AuthenticateResult.Fail("No password supplied."));
        }
        
        var expected = _config["AppSettings:Secret_ApiPassword"];
        
        if (string.IsNullOrEmpty(expected))
        {
            Logger.LogError("Configured API password is missing.");
            return Task.FromResult(AuthenticateResult.Fail("Server configuration error."));
        }
        
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        
        if (CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "PushApiClient")], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
        
        Logger.LogWarning("Invalid API password received.");
        
        if (Options.SendInvalidKeyMail && _throttleHelper.TryClaim(Options.InvalidKeyMailInterval))
        {
            try
            {
                InvalidKeyMailHelper.SendErrorMail("Invalid API password received.", null, _config);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send notification.");
            }
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid password."));
    }
}