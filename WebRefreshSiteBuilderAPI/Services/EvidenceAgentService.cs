using Newtonsoft.Json;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Pipeline;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Helpers;
using HtmlAgilityPack;

namespace WebRefreshSiteBuilderAPI.Services;

/// <summary>Runs independent specialists from the same evidence; none can mutate crawler facts.</summary>
public sealed class EvidenceAgentService
{
    private readonly IAgentService _agents;
    public EvidenceAgentService(IAgentService agents) => _agents = agents;

    public async Task PopulateAsync(EvidenceBundle bundle, CancellationToken ct = default)
    {
        await AnalyzeContentAsync(bundle, ct);
        await Task.WhenAll(AnalyzeStructureAsync(bundle, ct), AnalyzeVisualAsync(bundle, ct));
        await AnalyzeNavigationAsync(bundle, ct);
    }

    public async Task<ContentAnalysis> AnalyzeContentAsync(EvidenceBundle bundle, CancellationToken ct = default)
    {
        // Content preservation needs source markup, but sending every crawled
        // page together can exceed the context window. Process pages separately
        // and retain a raw response for each one.
        var tasks = bundle.Pages.Select(page => AnalyzeContentPageAsync(page, ct));
        var results = await Task.WhenAll(tasks);
        var pages = results.Select(result => result.Page).ToList();

        for (var index = 0; index < pages.Count; index++)
        {
            var page = pages[index];
            // URL identity is crawler-owned; do not let a model accidentally
            // merge pages or substitute a guessed URL.
            if (string.IsNullOrWhiteSpace(page.Url) && index < bundle.Pages.Count)
                page.Url = bundle.Pages[index].Url;
            page.SuggestedFileName = HtmlAnalyzerHelper.DeriveFileName(page.Url, pages.Count);
        }
        return bundle.Content = new ContentAnalysis
        {
            Pages = pages,
            RawResponse = JsonConvert.SerializeObject(results.Select(result => new { result.Url, result.RawResponse }), JsonSerializerHelper.CamelCaseSettings)
        };
    }

    public async Task<AgentAnalysis> AnalyzeStructureAsync(EvidenceBundle bundle, CancellationToken ct = default)
        => bundle.Structure = new AgentAnalysis { RawResponse = await SendAsync(AgentStage.Structure, Prompts.Structure, bundle, ct) };

    public async Task<AgentAnalysis> AnalyzeVisualAsync(EvidenceBundle bundle, CancellationToken ct = default)
        => bundle.Visual = new AgentAnalysis { RawResponse = await SendAsync(AgentStage.Visual, Prompts.Visual, bundle, ct) };

    public async Task<NavigationAnalysis> AnalyzeNavigationAsync(EvidenceBundle bundle, CancellationToken ct = default)
    {
        var raw = await SendAsync(AgentStage.Navigation, Prompts.Navigation, bundle, ct);
        // The Navigation Agent identifies candidates, but the hand-off is
        // deterministic: only crawler-observed pages may become local links.
        _ = JsonConvert.DeserializeObject<List<NavItem>>(HtmlAnalyzerHelper.StripCodeFence(raw), JsonSerializerHelper.CamelCaseSettings);
        return bundle.Navigation = new NavigationAnalysis { Items = HtmlAnalyzerHelper.BuildInternalNav(bundle.Content.Pages), RawResponse = raw };
    }

    private async Task<(string Url, PageSpec Page, string RawResponse)> AnalyzeContentPageAsync(PageEvidence page, CancellationToken ct)
    {
        var raw = await _agents.SendMessage(
            AgentStage.Content,
            Prompts.Content,
            JsonConvert.SerializeObject(ToAgentPage(page, includeHtml: true), JsonSerializerHelper.CamelCaseSettings),
            ct);
        var spec = JsonConvert.DeserializeObject<PageSpec>(HtmlAnalyzerHelper.StripCodeFence(raw), JsonSerializerHelper.CamelCaseSettings)
                   ?? new PageSpec();
        spec.Url = page.Url;
        return (page.Url, spec, raw);
    }

    private Task<string> SendAsync(AgentStage stage, string prompt, EvidenceBundle bundle, CancellationToken ct)
    {
        var includeHtml = stage == AgentStage.Structure;
        // ScreenshotBase64 is intentionally excluded: IAgentClient sends text,
        // so base64 cannot be visually interpreted and only consumes context.
        var input = bundle.Pages.Select(page => ToAgentPage(page, includeHtml));
        return _agents.SendMessage(stage, prompt, JsonConvert.SerializeObject(input, JsonSerializerHelper.CamelCaseSettings), ct);
    }

    private static object ToAgentPage(PageEvidence page, bool includeHtml) => new
    {
        page.Url,
        Html = includeHtml ? SanitiseHtml(page.Html) : null,
        page.Colors,
        Assets = page.Assets.Where(asset => !asset.Url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)),
        page.Links,
        page.Dom,
        page.Layout,
        page.BrowserError
    };

    private static string SanitiseHtml(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        foreach (var node in document.DocumentNode.SelectNodes("//script|//style|//noscript|//template|//svg") ?? Enumerable.Empty<HtmlNode>())
            node.Remove();
        foreach (var attribute in document.DocumentNode.SelectNodes("//*[@src or @href]")?.SelectMany(node => node.Attributes) ?? Enumerable.Empty<HtmlAttribute>())
            if ((attribute.Value ?? string.Empty).StartsWith("data:", StringComparison.OrdinalIgnoreCase)) attribute.Value = string.Empty;
        return document.DocumentNode.SelectSingleNode("//body")?.OuterHtml ?? document.DocumentNode.OuterHtml;
    }

    private static class Prompts
    {
        public const string Content = "You are the Content Agent. From evidence for one page, return ONLY one PageSpec JSON object. Preserve all source text, images and links; never invent content. Set url from evidence.";
        public const string Structure = "You are the Structure Agent. Inspect evidence JSON and return concise JSON describing page hierarchy, repeated components and semantic landmarks. Do not invent content.";
        public const string Visual = "You are the Visual Agent. Inspect deterministic colors, asset facts, browser status, and layout rectangles in evidence JSON. Return concise JSON visual observations only; distinguish facts from uncertainty.";
        public const string Navigation = "You are the Navigation Agent. From evidence JSON return ONLY a JSON array of NavItem. Include only internal links observed in source, preserving labels and resolve hrefs to generated filenames when possible.";
    }
}
