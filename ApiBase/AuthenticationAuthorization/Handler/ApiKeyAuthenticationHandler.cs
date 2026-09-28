using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using ApiBase.AuthenticationAuthorization.Helper;
using ApiBase.AuthenticationAuthorization.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiBase.AuthenticationAuthorization.Handler;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _config;
    private readonly DevMailThrottleHelper _throttleHelper;
    
    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory loggerFactory, UrlEncoder encoder, IConfiguration config, DevMailThrottleHelper throttleHelper) : base(options, loggerFactory, encoder)
    {
        _config = config;
        _throttleHelper = throttleHelper;
    }
    
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }
        
        string? authHeader = Request.Headers.Authorization;

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith(ApiKeyAuthenticationOptions.HeaderPrefix))
            return Task.FromResult(AuthenticateResult.Fail("Missing or malformed API key header."));

        var providedKey = authHeader[ApiKeyAuthenticationOptions.HeaderPrefix.Length..];
        var expectedKey = _config["AppSettings:Secret_ApiKey"];

        if (string.IsNullOrEmpty(expectedKey) || expectedKey.Length < 64)
        {
            Logger.LogError("Configured API key is missing or too short.");
            return Task.FromResult(AuthenticateResult.Fail("Server configuration error."));
        }

        if (string.IsNullOrEmpty(providedKey))
            return Task.FromResult(AuthenticateResult.Fail("No API key."));

        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);

        if (CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "ApiClient") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        var obfuscated = Obfuscate(providedKey);
        var invalidKeyMsg = $"Invalid API key received: {obfuscated}";
        Logger.LogWarning(invalidKeyMsg);

        if (Options.SendInvalidKeyMail && _throttleHelper.TryClaim(Options.MailThrottleInterval))
        {
            try
            {
                InvalidKeyMailHelper.SendErrorMail(invalidKeyMsg, null, _config);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send invalid-key notification.");
            }
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
    }

    private static string Obfuscate(string key)
    {
        return key.Length >= 20 ? $"{key[..5]}...{key[^5..]}" : "***";
    }
        
}