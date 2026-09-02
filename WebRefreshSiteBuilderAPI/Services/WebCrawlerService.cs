using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using SiteBuilderContracts.Crawling;
using SiteBuilderContracts.Requests;

namespace WebRefreshSiteBuilderAPI.Services;

public class WebCrawlerService
{
    private readonly HttpClient _httpClient;
    private readonly CrawlOptions _options;
    private readonly ILogger<WebCrawlerService> _logger;

    public WebCrawlerService(HttpClient httpClient, IOptions<CrawlOptions> options, ILogger<WebCrawlerService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _logger = logger;
    }

    public async Task<HtmlAnalysisRequest> CrawlAsync(string rootUrl, int? maxPagesOverride = null, CancellationToken ct = default)
    {
        var maxPages = maxPagesOverride ?? _options.MaxPages;
        var rootUri = new Uri(rootUrl);

        var visited = new HashSet<string>();
        var toVisit = new Queue<(string Url, int Depth)>();
        var pages = new List<PageInput>();

        toVisit.Enqueue((rootUrl, 0));

        while (toVisit.Count > 0 && pages.Count < maxPages)
        {
            var (url, depth) = toVisit.Dequeue();

            if (!visited.Add(url))
                continue;

            string html;
            try
            {
                html = await _httpClient.GetStringAsync(url, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch {Url}, skipping", url);
                continue;
            }

            // new — fetch linked stylesheets for this page before constructing PageInput
            var stylesheets = await FetchLinkedStylesheetsAsync(html, new Uri(url), ct);

            pages.Add(new PageInput { Url = url, Html = html, StylesheetContents = stylesheets });

            if (depth >= _options.MaxDepth)
                continue;

            foreach (var link in ExtractSameDomainLinks(html, rootUri))
            {
                if (!visited.Contains(link))
                    toVisit.Enqueue((link, depth + 1));
            }
        }

        return new HtmlAnalysisRequest { Pages = pages };
    }

    // new
    private async Task<List<string>> FetchLinkedStylesheetsAsync(string html, Uri pageUri, CancellationToken ct)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var hrefs = doc.DocumentNode
            .SelectNodes("//link[@rel='stylesheet'][@href]")
            ?.Select(n => n.GetAttributeValue("href", ""))
            ?? Enumerable.Empty<string>();

        var results = new List<string>();
        foreach (var href in hrefs)
        {
            if (!Uri.TryCreate(pageUri, href, out var absoluteUri))
                continue;

            try
            {
                results.Add(await _httpClient.GetStringAsync(absoluteUri, ct));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch stylesheet {Url}, skipping", absoluteUri);
            }
        }

        return results;
    }

    private static IEnumerable<string> ExtractSameDomainLinks(string html, Uri rootUri)
    {
        // unchanged
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var hrefs = doc.DocumentNode
            .SelectNodes("//a[@href]")
            ?.Select(a => a.GetAttributeValue("href", ""))
            ?? Enumerable.Empty<string>();

        foreach (var href in hrefs)
        {
            if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#") || href.StartsWith("mailto:"))
                continue;

            if (!Uri.TryCreate(rootUri, href, out var absoluteUri))
                continue;

            if (absoluteUri.Host.Equals(rootUri.Host, StringComparison.OrdinalIgnoreCase)
                && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
            {
                yield return absoluteUri.GetLeftPart(UriPartial.Query);
            }
        }
    }
}