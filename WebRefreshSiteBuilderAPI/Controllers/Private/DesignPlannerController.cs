using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteBuilderContracts.Pipeline;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshSiteBuilderAPI.Controllers.Private;

[Authorize]
[ApiController]
[Route("DesignPlanner")]
public sealed class DesignPlannerController : ControllerBase
{
    private readonly DesignPlannerService _planner;
    public DesignPlannerController(DesignPlannerService planner) => _planner = planner;

    [HttpPost("Plan")]
    public async Task<ActionResult<DesignPlan>> PlanAsync([FromBody] EvidenceBundle evidence, CancellationToken ct)
    {
        if (evidence.Content.Pages.Count == 0) return BadRequest("Run the Content stage before planning.");
        return Ok(await _planner.PlanAsync(evidence, ct));
    }
}
