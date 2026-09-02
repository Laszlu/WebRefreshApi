using System.ComponentModel.DataAnnotations;

namespace SiteBuilderContracts.Requests;

public class HtmlAnalysisRequest
{
    public List<PageInput> Pages { get; set; } = new();
}

public class PageInput
{
    public string Url { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public List<string> StylesheetContents { get; set; } = new();
}