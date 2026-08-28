using System.ComponentModel.DataAnnotations;

namespace SiteBuilderContracts.Requests;

public class HtmlAnalysisRequest
{
    public List<PageInput> Pages { get; set; } = new();
}

public class PageInput
{
    public string Url { get; set; } = string.Empty;   // used for nav-matching and file naming, not fetched
    public string Html { get; set; } = string.Empty;
}