using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteBuilderContracts.Requests;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshSiteBuilderAPI.Controllers.Private;

[Authorize]
[ApiController]
[Route("HtmlAnalyzer")]
public class HtmlAnalyzerController : ControllerBase
{
    private readonly HtmlAnalyzerService _htmlAnalyzerService;

    public HtmlAnalyzerController(HtmlAnalyzerService htmlAnalyzerService)
    {
        _htmlAnalyzerService = htmlAnalyzerService;
    }
    
    [HttpPost("Analyze")]
    public async Task<IActionResult> AnalyzeHtmlAsync([FromBody] HtmlAnalysisRequest request)
    {
        return (await _htmlAnalyzerService.AnalyzeHtmlAsync(request)).ToActionResult();
    }
}