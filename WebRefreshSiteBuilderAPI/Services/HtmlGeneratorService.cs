using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Requests;
using SiteBuilderContracts.Responses;
using WebRefreshSiteBuilderAPI.Data;

namespace WebRefreshSiteBuilderAPI.Services;

public class HtmlGeneratorService
{
    private readonly ILogger<HtmlGeneratorService> _logger;
    private readonly AgentService _agentService;

    private const string DefaultDesignSpecPath = "DesignSpec/style-guide.md";
    private const string PromptPath = "Prompts/HtmlGenerate.md";

    public HtmlGeneratorService(AgentService agentService, ILogger<HtmlGeneratorService> logger)
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
        var files = ParseFiles(rawResponse);

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

    private static List<SiteFile> ParseFiles(string response)
    {
        var files = new List<SiteFile>();
        var matches = Regex.Matches(response, @"```(\w+):(\S+)\r?\n(.*?)```", RegexOptions.Singleline);

        foreach (Match match in matches)
            files.Add(new SiteFile { FileName = match.Groups[2].Value, Content = match.Groups[3].Value });

        return files;
    }
}