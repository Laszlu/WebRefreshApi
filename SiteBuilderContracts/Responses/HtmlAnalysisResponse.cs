using SiteBuilderContracts.Generation;

namespace SiteBuilderContracts.Responses;

public class HtmlAnalysisResponse
{
    public SiteSpec SiteSpec { get; set; } = new();
    public List<PageAnalysis> RawPages { get; set; } = new();
}

public class PageAnalysis
{
    public string Url { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
}