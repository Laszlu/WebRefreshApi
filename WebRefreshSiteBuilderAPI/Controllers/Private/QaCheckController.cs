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

    public QaCheckController(QaCheckService qaCheckService)
    {
        _qaCheckService = qaCheckService;
    }

    [HttpPost("Run")]
    public async Task<IActionResult> RunQaAsync([FromBody] QaRequest request)
    {
        return (await _qaCheckService.RunQaAsync(request)).ToActionResult();
    }
}