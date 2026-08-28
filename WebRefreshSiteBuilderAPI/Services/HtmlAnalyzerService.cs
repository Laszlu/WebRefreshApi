using System.Text.Json;
using SiteBuilderContracts;
using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Requests;
using SiteBuilderContracts.Responses;
using WebRefreshSiteBuilderAPI.Data;

namespace WebRefreshSiteBuilderAPI.Services;

public class HtmlAnalyzerService
{
    private readonly ILogger<HtmlAnalyzerService> _logger;
    private readonly AgentService _agentService;

    public HtmlAnalyzerService(AgentService agentService, ILogger<HtmlAnalyzerService> logger)
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
            var raw = await _agentService.SendHtmlExtractMessage(prompt, page.Html);
            rawPages.Add(new PageAnalysis { Url = page.Url, Response = raw });

            var parsed = TryParsePageSpec(raw);
            if (parsed == null)
            {
                _logger.LogWarning("Failed to parse stage 1 JSON for {Url}", page.Url);
                continue;
            }

            parsed.Url = page.Url;
            parsed.SuggestedFileName = DeriveFileName(page.Url, request.Pages.Count);
            pageSpecs.Add(parsed);
        }

        var siteSpec = new SiteSpec
        {
            Pages = pageSpecs,
            Nav = BuildInternalNav(pageSpecs)
        };

        return SiteBuilderApiResultWithPayload<HtmlAnalysisResponse>.Success(new HtmlAnalysisResponse
        {
            SiteSpec = siteSpec,
            RawPages = rawPages
        });
    }

    private static PageSpec? TryParsePageSpec(string raw)
    {
        try
        {
            var json = StripCodeFence(raw);
            return JsonSerializer.Deserialize<PageSpec>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string StripCodeFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```"))
            return trimmed;

        var firstNewline = trimmed.IndexOf('\n');
        trimmed = firstNewline >= 0 ? trimmed[(firstNewline + 1)..] : trimmed;
        return trimmed.EndsWith("```") ? trimmed[..^3].Trim() : trimmed.Trim();
    }

    // Single-page requests still get "index.html"; multi-page requests derive
    // a filename from the URL path so pages don't collide.
    private static string DeriveFileName(string url, int totalPageCount)
    {
        if (totalPageCount == 1 || string.IsNullOrWhiteSpace(url))
            return "index.html";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "index.html";

        var path = uri.AbsolutePath.Trim('/');
        return string.IsNullOrEmpty(path) ? "index.html" : $"{path.Replace('/', '-').ToLowerInvariant()}.html";
    }

    // Keeps only nav links that point to pages actually included in this
    // request. Links to pages not supplied are dropped rather than carried
    // into the generated site as dead links.
    private static List<NavItem> BuildInternalNav(List<PageSpec> pages)
    {
        var urlToFileName = pages
            .Where(p => !string.IsNullOrWhiteSpace(p.Url))
            .ToDictionary(p => p.Url, p => p.SuggestedFileName);

        var nav = new List<NavItem>();
        var seen = new HashSet<string>();

        foreach (var page in pages)
        {
            foreach (var navItem in page.Nav)
            {
                var resolved = ResolveHref(navItem.Href, page.Url);
                if (resolved == null || !urlToFileName.TryGetValue(resolved, out var fileName))
                    continue;

                if (seen.Add(fileName))
                    nav.Add(new NavItem { Label = navItem.Label, Href = fileName });
            }
        }

        return nav;
    }

    private static string? ResolveHref(string href, string pageUrl)
    {
        if (string.IsNullOrWhiteSpace(pageUrl) || !Uri.TryCreate(pageUrl, UriKind.Absolute, out var baseUri))
            return null;

        return Uri.TryCreate(baseUri, href, out var absolute)
            ? absolute.GetLeftPart(UriPartial.Query)
            : null;
    }
}