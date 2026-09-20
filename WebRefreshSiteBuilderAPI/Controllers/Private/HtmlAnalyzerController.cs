using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteBuilderContracts.Requests;
using SiteBuilderContracts.Pipeline;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshSiteBuilderAPI.Controllers.Private;

[Authorize]
[ApiController]
[Route("HtmlAnalyzer")]
public class HtmlAnalyzerController : ControllerBase
{
    private readonly HtmlAnalyzerService _htmlAnalyzerService;
    private readonly DeterministicExtractionService _extraction;
    private readonly EvidenceAgentService _evidenceAgents;

    public HtmlAnalyzerController(HtmlAnalyzerService htmlAnalyzerService, DeterministicExtractionService extraction, EvidenceAgentService evidenceAgents)
    {
        _htmlAnalyzerService = htmlAnalyzerService;
        _extraction = extraction;
        _evidenceAgents = evidenceAgents;
    }
    
    [HttpPost("Analyze")]
    public async Task<IActionResult> AnalyzeHtmlAsync([FromBody] HtmlAnalysisRequest request)
    {
        return (await _htmlAnalyzerService.AnalyzeHtmlAsync(request)).ToActionResult();
    }

    // The following endpoints operate on the same EvidenceBundle. Send each
    // response as the next request body to inspect or replace a stage by hand.
    [HttpPost("Extract")]
    public async Task<ActionResult<EvidenceBundle>> ExtractAsync([FromBody] HtmlAnalysisRequest request, CancellationToken ct)
    {
        if (request.Pages.Count == 0) return BadRequest("At least one crawled page is required.");
        return Ok(new EvidenceBundle { Pages = await _extraction.ExtractAsync(request, ct) });
    }

    [HttpPost("Content")]
    public async Task<ActionResult<EvidenceBundle>> ContentAsync([FromBody] EvidenceBundle evidence, CancellationToken ct)
    {
        await _evidenceAgents.AnalyzeContentAsync(evidence, ct);
        return Ok(evidence);
    }

    [HttpPost("Structure")]
    public async Task<ActionResult<EvidenceBundle>> StructureAsync([FromBody] EvidenceBundle evidence, CancellationToken ct)
    {
        await _evidenceAgents.AnalyzeStructureAsync(evidence, ct);
        return Ok(evidence);
    }

    [HttpPost("Visual")]
    public async Task<ActionResult<EvidenceBundle>> VisualAsync([FromBody] EvidenceBundle evidence, CancellationToken ct)
    {
        await _evidenceAgents.AnalyzeVisualAsync(evidence, ct);
        return Ok(evidence);
    }

    [HttpPost("Navigation")]
    public async Task<ActionResult<EvidenceBundle>> NavigationAsync([FromBody] EvidenceBundle evidence, CancellationToken ct)
    {
        if (evidence.Content.Pages.Count == 0) return BadRequest("Run the Content stage before Navigation.");
        await _evidenceAgents.AnalyzeNavigationAsync(evidence, ct);
        return Ok(evidence);
    }
}
