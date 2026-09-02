using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteBuilderContracts.Requests;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshSiteBuilderAPI.Controllers.Private;

[Authorize]
[ApiController]
[Route("Crawler")]
public class CrawlerController : ControllerBase
{
    private readonly WebCrawlerService _crawlerService;

    public CrawlerController(WebCrawlerService crawlerService)
    {
        _crawlerService = crawlerService;
    }

    [HttpPost("Crawl")]
    public async Task<IActionResult> CrawlAsync([FromBody] CrawlRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return BadRequest("A valid absolute URL is required.");

        var result = await _crawlerService.CrawlAsync(request.Url, request.MaxPagesOverride, ct);
        return Ok(result);
    }
}