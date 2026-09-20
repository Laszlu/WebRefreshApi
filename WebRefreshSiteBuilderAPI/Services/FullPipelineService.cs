using SiteBuilderContracts.Pipeline;
using SiteBuilderContracts.Requests;
using SiteBuilderContracts.Responses;
using WebRefreshSiteBuilderAPI.Data;

namespace WebRefreshSiteBuilderAPI.Services;

public sealed class FullPipelineService
{
    private readonly WebCrawlerService _crawler;
    private readonly DeterministicExtractionService _extraction;
    private readonly EvidenceAgentService _agents;
    private readonly DesignPlannerService _planner;
    private readonly HtmlGeneratorService _generator;
    private readonly QaCheckService _qa;
    private readonly VisualQaService _visualQa;

    public FullPipelineService(WebCrawlerService crawler, DeterministicExtractionService extraction, EvidenceAgentService agents, DesignPlannerService planner, HtmlGeneratorService generator, QaCheckService qa, VisualQaService visualQa)
        => (_crawler, _extraction, _agents, _planner, _generator, _qa, _visualQa) = (crawler, extraction, agents, planner, generator, qa, visualQa);

    public async Task<SiteBuilderApiResultWithPayload<FullPipelineResponse>> RunAsync(FullPipelineRequest request, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return SiteBuilderApiResultWithPayload<FullPipelineResponse>.Fail(SiteBuilderActionResult.RequestContentMissing, null, "A valid absolute URL is required.");
        
        var crawled = await _crawler.CrawlAsync(request.Url, request.MaxPagesOverride, ct);
        if (crawled.Pages.Count == 0)
            return SiteBuilderApiResultWithPayload<FullPipelineResponse>.Fail(SiteBuilderActionResult.RequestContentMissing, null, "Crawler could not retrieve any pages.");
        
        var evidence = new EvidenceBundle { Pages = await _extraction.ExtractAsync(crawled, ct) };
        
        await _agents.PopulateAsync(evidence, ct);
        
        var plan = await _planner.PlanAsync(evidence, ct);
        
        var generated = await _generator.GenerateHtmlAsync(new() { SiteSpec = plan.SiteSpec, DesignSpecOverride = request.DesignSpecOverride });
        
        var files = generated.Payload!.Files;
        
        var qa = (await _qa.RunQaAsync(new() { Files = files, ExpectedSiteSpec = plan.SiteSpec })).Payload!;
        
        var visual = await _visualQa.RunAsync(files, ct);
        
        var attempts = 0;
        
        while ((!qa.Passed || !visual.Passed) && attempts < Math.Clamp(request.MaxRepairAttempts, 0, 3))
        {
            attempts++;
            var repair = string.Join('\n', qa.Issues.Concat(visual.Issues));
            
            generated = await _generator.GenerateHtmlAsync(new() { SiteSpec = plan.SiteSpec, DesignSpecOverride = request.DesignSpecOverride, RepairInstructions = repair });
            
            files = generated.Payload!.Files;
            
            qa = (await _qa.RunQaAsync(new() { Files = files, ExpectedSiteSpec = plan.SiteSpec })).Payload!;
            
            visual = await _visualQa.RunAsync(files, ct);
        }
        
        return SiteBuilderApiResultWithPayload<FullPipelineResponse>.Success(new FullPipelineResponse { Evidence = evidence, Plan = plan, Files = files, DeterministicQa = qa, VisualQa = visual, RepairAttempts = attempts });
    }
}
