namespace SiteBuilderContracts.Crawling;

public class CrawlOptions
{
    public const string SectionName = "Crawl";
    public int MaxPages { get; set; } = 5;
    public int MaxDepth { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 10;
}