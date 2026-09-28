using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Pipeline;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Helpers;

namespace WebRefreshSiteBuilderAPI.Services;

public sealed class DesignPlannerService
{
    private readonly IAgentService _agents;
    public DesignPlannerService(IAgentService agents) => _agents = agents;

    public async Task<DesignPlan> PlanAsync(EvidenceBundle evidence, CancellationToken ct = default)
    {
        var input = JsonConvert.SerializeObject(evidence, JsonSerializerHelper.CamelCaseSettings);
        
        const string prompt = "You are the Design Planner. Turn the evidence bundle into ONLY valid JSON matching DesignPlan: siteSpec, informationArchitecture, components, layoutStrategy, responsiveStrategy. Preserve source content and observed navigation. Do not invent copy, URLs, assets or brand colors.";
        
        var raw = await _agents.SendMessage(AgentStage.Plan, prompt, input, ct);
        
        var plan = DeserializePlan(raw);
        
        plan.RawResponse = raw;
        
        // The planner owns design decisions, not source content. Models often
        // emit page-shaped placeholders, which are non-empty lists but contain
        // no usable title, URL, or sections. Keep Content Agent output as the
        // trustworthy hand-off in that case.
        if (HasUnusablePages(plan.SiteSpec.Pages, evidence.Content.Pages.Count))
            plan.SiteSpec.Pages = evidence.Content.Pages;

        // Navigation was already resolved against crawled URLs, so it is the
        // authoritative source for generated local links.
        if (plan.SiteSpec.Nav.Count == 0) plan.SiteSpec.Nav = evidence.Navigation.Items;
        
        if (plan.SiteSpec.Brand.RawColorHints.Count == 0)
        {
            var colors = evidence.Pages.SelectMany(p => p.Colors).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            
            plan.SiteSpec.Brand = new BrandSignals { PrimaryColor = colors.ElementAtOrDefault(0), AccentColor = colors.ElementAtOrDefault(1), BackgroundColor = colors.ElementAtOrDefault(2), RawColorHints = colors, Source = colors.Count > 0 ? "css" : "none" };
        }
        return plan;
    }

    private static bool HasUnusablePages(IReadOnlyCollection<PageSpec> pages, int expectedCount) =>
        pages.Count != expectedCount || pages.Any(page =>
            string.IsNullOrWhiteSpace(page.Url)
            || string.IsNullOrWhiteSpace(page.SuggestedFileName)
            || string.IsNullOrWhiteSpace(page.PageTitle)
            || page.Sections.Count == 0);

    private static DesignPlan DeserializePlan(string raw)
    {
        var json = HtmlAnalyzerHelper.StripCodeFence(raw);
        var document = JObject.Parse(json);

        // Models naturally tend to express IA and layout strategies as nested
        // objects. The public contract retains string fields for compatibility,
        // so preserve those valid structured values as compact JSON instead of
        // rejecting the entire plan.
        foreach (var propertyName in new[] { "informationArchitecture", "layoutStrategy", "responsiveStrategy" })
        {
            var token = document[propertyName];
            if (token is JArray or JObject)
                document[propertyName] = token.ToString(Formatting.None);
        }

        if (document["components"] is JArray components)
        {
            for (var index = 0; index < components.Count; index++)
            {
                if (components[index] is JArray or JObject)
                    components[index] = components[index]!.ToString(Formatting.None);
            }
        }

        return document.ToObject<DesignPlan>(JsonSerializer.Create(JsonSerializerHelper.CamelCaseSettings)) ?? new DesignPlan();
    }
}
