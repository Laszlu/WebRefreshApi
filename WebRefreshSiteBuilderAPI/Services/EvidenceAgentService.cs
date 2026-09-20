using Newtonsoft.Json;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Pipeline;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Helpers;

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
        var raw = await SendAsync(AgentStage.Content, Prompts.Content, bundle, ct);

        var pages = JsonConvert.DeserializeObject<List<PageSpec>>(HtmlAnalyzerHelper.StripCodeFence(raw), JsonSerializerHelper.CamelCaseSettings) ?? [];
        for (var index = 0; index < pages.Count; index++)
        {
            var page = pages[index];
            // URL identity is crawler-owned; do not let a model accidentally
            // merge pages or substitute a guessed URL.
            if (string.IsNullOrWhiteSpace(page.Url) && index < bundle.Pages.Count)
                page.Url = bundle.Pages[index].Url;
            page.SuggestedFileName = HtmlAnalyzerHelper.DeriveFileName(page.Url, pages.Count);
        }
        return bundle.Content = new ContentAnalysis { Pages = pages, RawResponse = raw };
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

    private Task<string> SendAsync(AgentStage stage, string prompt, EvidenceBundle bundle, CancellationToken ct) =>
        _agents.SendMessage(stage, prompt, JsonConvert.SerializeObject(bundle.Pages, JsonSerializerHelper.CamelCaseSettings), ct);

    private static class Prompts
    {
        public const string Content = "You are the Content Agent. From the evidence JSON, return ONLY a JSON array of PageSpec objects. Preserve all source text, images and links; never invent content. Set url from evidence.";
        public const string Structure = "You are the Structure Agent. Inspect evidence JSON and return concise JSON describing page hierarchy, repeated components and semantic landmarks. Do not invent content.";
        public const string Visual = "You are the Visual Agent. Inspect deterministic colors, asset facts, screenshots and layout rectangles in evidence JSON. Return concise JSON visual observations only; distinguish facts from uncertainty.";
        public const string Navigation = "You are the Navigation Agent. From evidence JSON return ONLY a JSON array of NavItem. Include only internal links observed in source, preserving labels and resolve hrefs to generated filenames when possible.";
    }
}
