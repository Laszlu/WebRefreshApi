using System.Text.Json;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Requests;
using SiteBuilderContracts.Responses;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Helpers;

namespace WebRefreshSiteBuilderAPI.Services;

public class HtmlAnalyzerService
{
    private readonly ILogger<HtmlAnalyzerService> _logger;
    private readonly IAgentService _agentService;

    public HtmlAnalyzerService(IAgentService agentService, ILogger<HtmlAnalyzerService> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    public async Task<SiteBuilderApiResultWithPayload<HtmlAnalysisResponse>> AnalyzeHtmlAsync(HtmlAnalysisRequest request)
{
    if (request.Pages.Count == 0)
        return SiteBuilderApiResultWithPayload<HtmlAnalysisResponse>.Fail(SiteBuilderActionResult.RequestContentMissing, null, "At least one page is required.");

    var prompt = await File.ReadAllTextAsync("Prompts/HtmlExtract.md");
    var rawPages = new List<PageAnalysis>();
    var pageSpecs = new List<PageSpec>();

    foreach (var page in request.Pages)
    {
        // Deterministic pass first — no API call, no cost, no hallucination risk.
        var cssColors = page.StylesheetContents
            .SelectMany(CssColorExtractionHelper.ExtractDeclaredColors)
            .Distinct()
            .ToList();

        var raw = await _agentService.SendHtmlExtractMessage(prompt, page.Html);
        rawPages.Add(new PageAnalysis { Url = page.Url, Response = raw });

        var parsed = HtmlAnalyzerHelper.TryParsePageSpec(raw);
        if (parsed == null)
        {
            _logger.LogWarning("Failed to parse stage 1 JSON for {Url}", page.Url);
            continue;
        }

        parsed.Url = page.Url;
        parsed.SuggestedFileName = HtmlAnalyzerHelper.DeriveFileName(page.Url, request.Pages.Count);

        // CSS-derived colors take priority over whatever the LLM inferred from
        // inline styles — deterministic and exact beats inferred and approximate.
        if (cssColors.Count > 0)
        {
            parsed.Brand.PrimaryColor ??= cssColors.ElementAtOrDefault(0);
            parsed.Brand.AccentColor ??= cssColors.ElementAtOrDefault(1);
            parsed.Brand.RawColorHints = cssColors;
            parsed.Brand.Source = "css";
        }
        else if (parsed.Brand.PrimaryColor != null || parsed.Brand.RawColorHints.Count > 0)
        {
            parsed.Brand.Source = "llm";
        }

        pageSpecs.Add(parsed);
    }

    var siteSpec = new SiteSpec
    {
        Pages = pageSpecs,
        Nav = HtmlAnalyzerHelper.BuildInternalNav(pageSpecs),
        Brand = CssColorExtractionHelper.ResolveBrandColors(pageSpecs)
    };

    return SiteBuilderApiResultWithPayload<HtmlAnalysisResponse>.Success(new HtmlAnalysisResponse
    {
        SiteSpec = siteSpec,
        RawPages = rawPages
    });
}
}