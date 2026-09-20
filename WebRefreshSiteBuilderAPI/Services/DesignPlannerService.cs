using Newtonsoft.Json;
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
        
        var plan = JsonConvert.DeserializeObject<DesignPlan>(HtmlAnalyzerHelper.StripCodeFence(raw), JsonSerializerHelper.CamelCaseSettings) ?? new DesignPlan();
        
        plan.RawResponse = raw;
        
        // The planner may describe well but omit a field. Evidence facts are the safe fallback.
        
        if (plan.SiteSpec.Pages.Count == 0) plan.SiteSpec.Pages = evidence.Content.Pages;
        
        if (plan.SiteSpec.Nav.Count == 0) plan.SiteSpec.Nav = evidence.Navigation.Items;
        
        if (plan.SiteSpec.Brand.RawColorHints.Count == 0)
        {
            var colors = evidence.Pages.SelectMany(p => p.Colors).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            
            plan.SiteSpec.Brand = new BrandSignals { PrimaryColor = colors.ElementAtOrDefault(0), AccentColor = colors.ElementAtOrDefault(1), BackgroundColor = colors.ElementAtOrDefault(2), RawColorHints = colors, Source = colors.Count > 0 ? "css" : "none" };
        }
        return plan;
    }
}
