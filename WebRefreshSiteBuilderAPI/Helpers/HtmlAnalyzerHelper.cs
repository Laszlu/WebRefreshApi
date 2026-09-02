using System.Text.Json;
using SiteBuilderContracts.Generation;

namespace WebRefreshSiteBuilderAPI.Helpers;

public class HtmlAnalyzerHelper
{
    public static PageSpec? TryParsePageSpec(string raw)
    {
        try
        {
            var json = StripCodeFence(raw);
            return JsonSerializer.Deserialize<PageSpec>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }
    
    public static string StripCodeFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```"))
            return trimmed;

        var firstNewline = trimmed.IndexOf('\n');
        trimmed = firstNewline >= 0 ? trimmed[(firstNewline + 1)..] : trimmed;
        return trimmed.EndsWith("```") ? trimmed[..^3].Trim() : trimmed.Trim();
    }
    
    // Single-page requests still get "index.html"; multi-page requests derive
    // a filename from the URL path so pages don't collide.
    public static string DeriveFileName(string url, int totalPageCount)
    {
        if (totalPageCount == 1 || string.IsNullOrWhiteSpace(url))
            return "index.html";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "index.html";

        var path = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrEmpty(path))
            return "index.html";

        var slug = path.Replace('/', '-').ToLowerInvariant();
        return slug.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ? slug : $"{slug}.html";
    }
    
    // Keeps only nav links that point to pages actually included in this
    // request. Links to pages not supplied are dropped rather than carried
    // into the generated site as dead links.
    public static List<NavItem> BuildInternalNav(List<PageSpec> pages)
    {
        var urlToFileName = pages
            .Where(p => !string.IsNullOrWhiteSpace(p.Url))
            .ToDictionary(p => p.Url, p => p.SuggestedFileName);

        var nav = new List<NavItem>();
        var seen = new HashSet<string>();

        foreach (var page in pages)
        {
            foreach (var navItem in page.Nav)
            {
                var resolved = ResolveHref(navItem.Href, page.Url);
                if (resolved == null || !urlToFileName.TryGetValue(resolved, out var fileName))
                    continue;

                if (seen.Add(fileName))
                    nav.Add(new NavItem { Label = navItem.Label, Href = fileName });
            }
        }

        return nav;
    }
    
    public static string? ResolveHref(string href, string pageUrl)
    {
        if (string.IsNullOrWhiteSpace(pageUrl) || !Uri.TryCreate(pageUrl, UriKind.Absolute, out var baseUri))
            return null;

        return Uri.TryCreate(baseUri, href, out var absolute)
            ? absolute.GetLeftPart(UriPartial.Query)
            : null;
    }
}