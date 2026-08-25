using Anthropic;
using ApiBase.AuthenticationAuthorization.Extensions;
using ApiBase.AuthenticationAuthorization.Options;
using ApiBase.ExceptionHandling.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteBuilderContracts.Config;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshSiteBuilderAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add services to the container.

        builder.Services.AddControllers();

        builder.Services.AddBaseExceptionHandler();
        
        builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
            .AddApiKeyAuthentication(o =>
            {
                o.MailThrottleInterval = TimeSpan.FromMinutes(10);
                o.SendInvalidKeyMail = true;
            });
        
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme)
                .RequireAuthenticatedUser()
                .Build());

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                var errors = context.ModelState
                    .Where(keyValuePair => keyValuePair.Value?.Errors.Count > 0)
                    .ToDictionary(
                        keyValuePair => keyValuePair.Key,
                        keyValuePair => string.Join("; ", keyValuePair.Value!.Errors.Select(e => e.ErrorMessage)));

                logger.LogWarning(
                    "Validation failed for {Path}: {@Errors}",
                    context.HttpContext.Request.Path, errors);

                return new BadRequestObjectResult(new { message = "Invalid request. Check server logs." });
            };
        });
        
        builder.Services
            .AddOptions<AnthropicOptions>()
            .Bind(builder.Configuration.GetSection(AnthropicOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Anthropic:ApiKey is missing")
            .ValidateOnStart();

        builder.Services.AddHttpClient<AnthropicClient>();

        builder.Services.AddScoped<AgentService>();
        builder.Services.AddScoped<HtmlAnalyzerService>();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseExceptionHandler();
        
        app.MapControllers();

        app.Run();
    }
}