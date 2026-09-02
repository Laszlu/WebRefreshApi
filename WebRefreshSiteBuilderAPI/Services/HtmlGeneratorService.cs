using Newtonsoft.Json;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Requests;
using SiteBuilderContracts.Responses;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Helpers;

namespace WebRefreshSiteBuilderAPI.Services;

public class HtmlGeneratorService
{
    private readonly ILogger<HtmlGeneratorService> _logger;
    private readonly IAgentService _agentService;

    private const string DefaultDesignSpecPath = "DesignSpec/style-guide.md";
    private const string PromptPath = "Prompts/HtmlGenerate.md";

    public HtmlGeneratorService(IAgentService agentService, ILogger<HtmlGeneratorService> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    public async Task<SiteBuilderApiResultWithPayload<HtmlGenerationResponse>> GenerateHtmlAsync(HtmlGenerationRequest request)
    {
        var prompt = await File.ReadAllTextAsync(PromptPath);
        var designSpec = request.DesignSpecOverride ?? await File.ReadAllTextAsync(DefaultDesignSpecPath);

        var siteSpecJson = JsonConvert.SerializeObject(request.SiteSpec);
        var userContent = siteSpecJson + "\n\n---DESIGN SPEC---\n" + designSpec;

        var rawResponse = await _agentService.SendHtmlGenerateMessage(prompt, userContent);
        var files = HtmlGeneratorHelper.ParseFiles(rawResponse);

        var expectedPageCount = request.SiteSpec.Pages.Count;
        var actualHtmlCount = files.Count(f => f.FileName.EndsWith(".html"));
        if (actualHtmlCount < expectedPageCount)
        {
            _logger.LogWarning(
                "Expected {Expected} HTML files, got {Actual}. Response may have been truncated or malformed.",
                expectedPageCount, actualHtmlCount);
        }

        return SiteBuilderApiResultWithPayload<HtmlGenerationResponse>.Success(new HtmlGenerationResponse
        {
            RawResponse = rawResponse,
            Files = files
        });
    }
}