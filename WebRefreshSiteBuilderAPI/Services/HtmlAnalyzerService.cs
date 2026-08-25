using SiteBuilderContracts;
using SiteBuilderContracts.Requests;
using SiteBuilderContracts.Responses;
using WebRefreshSiteBuilderAPI.Data;

namespace WebRefreshSiteBuilderAPI.Services;

public class HtmlAnalyzerService
{
    private readonly ILogger<HtmlAnalyzerService> _logger;
    
    private readonly AgentService _agentService;

    public HtmlAnalyzerService(AgentService agentService, ILogger<HtmlAnalyzerService> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }
    
    public async Task<SiteBuilderApiResultWithPayload<HtmlAnalysisResponse>> AnalyzeHtmlAsync(HtmlAnalysisRequest request)
    {
        var prompt = await File.ReadAllTextAsync("Prompts/HtmlExtract.md");

        var response = await _agentService.SendHtmlExtractMessage(prompt, request.Html);

        return SiteBuilderApiResultWithPayload<HtmlAnalysisResponse>.Success(new HtmlAnalysisResponse
        {
            Response = response
        });
    }
}