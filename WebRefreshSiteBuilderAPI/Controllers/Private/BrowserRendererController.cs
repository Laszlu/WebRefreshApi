using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteBuilderContracts.Requests;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshSiteBuilderAPI.Controllers.Private;

[Authorize]
[ApiController]
[Route("BrowserRenderer")]
public sealed class BrowserRendererController : ControllerBase
{
    private readonly BrowserRenderService _browser;
    public BrowserRendererController(BrowserRenderService browser) => _browser = browser;

    [HttpPost("Render")]
    public async Task<IActionResult> RenderAsync([FromBody] BrowserRenderRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Url)) return Ok(await _browser.RenderRemoteAsync(request.Url, ct));
        if (request.Files.Count > 0) return Ok(await _browser.RenderFilesAsync(request.Files, ct));
        return BadRequest("Supply either url or files.");
    }
}
