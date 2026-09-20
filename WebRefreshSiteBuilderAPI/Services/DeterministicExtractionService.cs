using HtmlAgilityPack;
using SiteBuilderContracts.Pipeline;
using SiteBuilderContracts.Requests;
using WebRefreshSiteBuilderAPI.Helpers;

namespace WebRefreshSiteBuilderAPI.Services;

/// <summary>Extracts facts only. This service deliberately contains no LLM calls.</summary>
public sealed class DeterministicExtractionService
{
    private readonly BrowserRenderService _browser;

    public DeterministicExtractionService(BrowserRenderService browser) => _browser = browser;

    public async Task<List<PageEvidence>> ExtractAsync(HtmlAnalysisRequest crawl, CancellationToken ct = default)
    {
        var tasks = crawl.Pages.Select(page => ExtractPageAsync(page, ct));
        return (await Task.WhenAll(tasks)).ToList();
    }

    private async Task<PageEvidence> ExtractPageAsync(PageInput page, CancellationToken ct)
    {
        var doc = new HtmlDocument();
        
        doc.LoadHtml(page.Html);
        
        var root = Uri.TryCreate(page.Url, UriKind.Absolute, out var uri) ? uri : null;
        
        var nodes = doc.DocumentNode.Descendants().Where(n => n.NodeType == HtmlNodeType.Element).ToList();
        
        var colors = CssColorExtractionHelper.ExtractSiteWideCssColors([page]).ToList();
        
        colors.AddRange(ExtractInlineColors(page.Html));

        var evidence = new PageEvidence
        {
            Url = page.Url,
            Html = page.Html,
            Colors = colors.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Dom = new DomMetrics
            {
                ElementCount = nodes.Count,
                TextCharacters = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText).Trim().Length,
                HeadingCount = nodes.Count(n => n.Name.Length == 2 && n.Name[0] == 'h' && char.IsDigit(n.Name[1])),
                ImageCount = nodes.Count(n => n.Name == "img"),
                LinkCount = nodes.Count(n => n.Name == "a")
            }
        };

        evidence.Assets = nodes.Where(n => n.Name is "img" or "script" or "source" or "link")
            .Select(n => new { Node = n, Value = n.GetAttributeValue(n.Name == "link" ? "href" : "src", "") })
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => new AssetEvidence { Url = Resolve(root, x.Value), Kind = x.Node.Name, Alt = x.Node.GetAttributeValue("alt", "") })
            .ToList();
        
        evidence.Links = nodes.Where(n => n.Name == "a")
            .Select(n => new LinkEvidence { Label = HtmlEntity.DeEntitize(n.InnerText).Trim(), Href = n.GetAttributeValue("href", "") })
            .Where(l => !string.IsNullOrWhiteSpace(l.Href))
            .Select(l => { l.IsInternal = root is not null && Uri.TryCreate(root, l.Href, out var target) && target.Host.Equals(root.Host, StringComparison.OrdinalIgnoreCase); return l; })
            .ToList();

        var render = await _browser.RenderRemoteAsync(page.Url, ct);
        
        evidence.ScreenshotBase64 = render.ScreenshotBase64;
        
        evidence.Layout = render.Layout;
        
        evidence.BrowserError = render.Error;
        
        return evidence;
    }

    private static string Resolve(Uri? root, string value) => root is not null && Uri.TryCreate(root, value, out var absolute) ? absolute.ToString() : value;
    private static IEnumerable<string> ExtractInlineColors(string html) => System.Text.RegularExpressions.Regex.Matches(html, @"(?i)(?:#(?:[0-9a-f]{3,8})\b|rgba?\([^)]*\)|hsla?\([^)]*\))").Select(m => m.Value);
}
