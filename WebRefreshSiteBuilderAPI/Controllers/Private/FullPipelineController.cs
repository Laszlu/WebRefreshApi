using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteBuilderContracts.Requests;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshSiteBuilderAPI.Controllers.Private;

[Authorize]
[ApiController]
[Route("Full")]
public class FullPipelineController : ControllerBase
{
    private readonly FullPipelineService _pipeline;
    public FullPipelineController(FullPipelineService pipeline) => _pipeline = pipeline;

    [HttpPost("Run")]
    public async Task<IActionResult> RunAsync([FromBody] FullPipelineRequest request, CancellationToken ct)
        => (await _pipeline.RunAsync(request, ct)).ToActionResult();
}
