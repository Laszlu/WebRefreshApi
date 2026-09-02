namespace SiteBuilderContracts.Requests;

public class CrawlRequest
{
    public string Url { get; set; } = string.Empty;
    public int? MaxPagesOverride { get; set; }
}