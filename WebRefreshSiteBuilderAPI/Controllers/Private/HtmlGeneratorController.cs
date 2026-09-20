using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteBuilderContracts.Requests;
using SiteBuilderContracts.Pipeline;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshSiteBuilderAPI.Controllers.Private;

[Authorize]
[ApiController]
[Route("HtmlGenerator")]
public class HtmlGeneratorController : ControllerBase
{
    private readonly HtmlGeneratorService _htmlGeneratorService;

    public HtmlGeneratorController(HtmlGeneratorService htmlGeneratorService)
    {
        _htmlGeneratorService = htmlGeneratorService;
    }

    [HttpPost("Generate")]
    public async Task<IActionResult> GenerateHtmlAsync([FromBody] HtmlGenerationRequest request)
    {
        return (await _htmlGeneratorService.GenerateHtmlAsync(request)).ToActionResult();
    }

    [HttpPost("GenerateFromPlan")]
    public async Task<IActionResult> GenerateFromPlanAsync([FromBody] PlanGenerationRequest request)
        => (await _htmlGeneratorService.GenerateHtmlAsync(new HtmlGenerationRequest
        {
            SiteSpec = request.Plan.SiteSpec,
            DesignSpecOverride = request.DesignSpecOverride,
            RepairInstructions = request.RepairInstructions
        })).ToActionResult();
}
