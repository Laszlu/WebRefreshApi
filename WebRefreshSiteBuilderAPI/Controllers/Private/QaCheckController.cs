using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteBuilderContracts.Requests;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshSiteBuilderAPI.Controllers.Private;

[Authorize]
[ApiController]
[Route("QaCheck")]
public class QaCheckController : ControllerBase
{
    private readonly QaCheckService _qaCheckService;
    private readonly VisualQaService _visualQaService;

    public QaCheckController(QaCheckService qaCheckService, VisualQaService visualQaService)
    {
        _qaCheckService = qaCheckService;
        _visualQaService = visualQaService;
    }

    [HttpPost("Run")]
    public async Task<IActionResult> RunQaAsync([FromBody] QaRequest request)
    {
        return (await _qaCheckService.RunQaAsync(request)).ToActionResult();
    }

    [HttpPost("Visual")]
    public async Task<IActionResult> RunVisualQaAsync([FromBody] QaRequest request, CancellationToken ct)
        => Ok(await _visualQaService.RunAsync(request.Files, ct));
}
