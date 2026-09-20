using Anthropic;
using Anthropic.Core;
using ApiBase.AuthenticationAuthorization.Extensions;
using ApiBase.AuthenticationAuthorization.Options;
using ApiBase.ExceptionHandling.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Config;
using SiteBuilderContracts.Crawling;
using WebRefreshSiteBuilderAPI.Agents;
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
        
        var provider = builder.Configuration["AgentProvider"];
        
        builder.Services.AddOptions<AnthropicOptions>()
            .Bind(builder.Configuration.GetSection(AnthropicOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ExtractModel) && !string.IsNullOrWhiteSpace(o.GenerateModel),
                "Anthropic ExtractModel and GenerateModel must both be set")
            .ValidateOnStart();
        
        builder.Services.AddOptions<OpenAiOptions>()
            .Bind(builder.Configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ExtractModel) && !string.IsNullOrWhiteSpace(o.GenerateModel),
                "OpenAi ExtractModel and GenerateModel must both be set")
            .ValidateOnStart();

        switch (provider)
        {
            case "Anthropic":
                builder.Services.AddSingleton(sp =>
                {
                    var apiKey = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value.ApiKey;
                    var clientOptions = new ClientOptions { ApiKey = apiKey };
                    return new AnthropicClient(clientOptions);
                });
                builder.Services.AddScoped<IAgentClient, AnthropicAgentClient>();
                builder.Services.AddScoped<IAgentModelResolver, AnthropicModelResolver>();
                break;

            case "OpenAi":
                builder.Services.AddSingleton(sp =>
                {
                    var apiKey = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value.ApiKey;
                    return new OpenAIClient(apiKey);
                });
                builder.Services.AddScoped<IAgentClient, OpenAiAgentClient>();
                builder.Services.AddScoped<IAgentModelResolver, OpenAiModelResolver>();
                break;

            default:
                throw new InvalidOperationException($"Unknown AgentProvider: {provider}");
        }
        
        builder.Services.AddOptions<CrawlOptions>()
            .Bind(builder.Configuration.GetSection(CrawlOptions.SectionName))
            .ValidateOnStart();

        builder.Services.AddHttpClient<WebCrawlerService>();

        builder.Services.AddScoped<IAgentService, AgentService>();
        builder.Services.AddSingleton<BrowserRenderService>();
        builder.Services.AddScoped<DeterministicExtractionService>();
        builder.Services.AddScoped<EvidenceAgentService>();
        builder.Services.AddScoped<DesignPlannerService>();
        builder.Services.AddScoped<VisualQaService>();
        builder.Services.AddScoped<FullPipelineService>();
        builder.Services.AddScoped<HtmlAnalyzerService>();
        builder.Services.AddScoped<HtmlGeneratorService>();
        builder.Services.AddScoped<QaCheckService>();

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
