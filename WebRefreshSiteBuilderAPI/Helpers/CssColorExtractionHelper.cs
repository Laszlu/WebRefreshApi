using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Requests;

namespace WebRefreshSiteBuilderAPI.Helpers;

public class CssColorExtractionHelper
{
    public static List<string> ExtractDeclaredColors(string css)
    {
        var colors = new List<string>();
        // CSS parser versions expose custom properties differently. A declaration
        // scan is deterministic and intentionally limited to literal color values.
        var root = System.Text.RegularExpressions.Regex.Match(css, @":root\s*\{(?<body>[^}]*)\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (root.Success)
            colors.AddRange(FindColorDeclarations(root.Groups["body"].Value, customPropertiesOnly: true));
        var frequency = new Dictionary<string, int>();
        foreach (var color in FindColorDeclarations(css, customPropertiesOnly: false))
            frequency[color] = frequency.GetValueOrDefault(color) + 1;

        colors.AddRange(frequency.OrderByDescending(kv => kv.Value).Take(5).Select(kv => kv.Key));

        return colors.Distinct().Take(10).ToList();
    }
    
    public static List<string> ExtractSiteWideCssColors(List<PageInput> pages)
    {
        var uniqueStylesheets = pages
            .SelectMany(p => p.StylesheetContents)
            .Distinct()
            .ToList();

        return uniqueStylesheets
            .SelectMany(ExtractDeclaredColors)
            .Distinct()
            .ToList();
    }

    private static bool LooksLikeColor(string value)
        => value.StartsWith('#') || value.StartsWith("rgb") || value.StartsWith("hsl");

    private static IEnumerable<string> FindColorDeclarations(string css, bool customPropertiesOnly)
    {
        if (customPropertiesOnly)
        {
            var variables = System.Text.RegularExpressions.Regex.Matches(css, @"(?i)--[\w-]+\s*:\s*(?<color>[^;}]+)");
            return variables.Select(m => m.Groups["color"].Value.Trim()).Where(LooksLikeColor);
        }
        var matches = System.Text.RegularExpressions.Regex.Matches(css, @"(?i)(?:color|background-color|border-color|fill|stroke)\s*:\s*(?<color>#[0-9a-f]{3,8}\b|rgba?\([^;}]+\)|hsla?\([^;}]+\))");
        return matches.Select(m => m.Groups["color"].Value).Where(LooksLikeColor);
    }
    
    public static BrandSignals ResolveBrandColors(List<PageSpec> pages, List<string> siteWideCssColors)
    {
        if (siteWideCssColors.Count > 0)
        {
            return new BrandSignals
            {
                PrimaryColor = siteWideCssColors.ElementAtOrDefault(0),
                AccentColor = siteWideCssColors.ElementAtOrDefault(1),
                BackgroundColor = siteWideCssColors.ElementAtOrDefault(2),
                RawColorHints = siteWideCssColors,
                Source = "css"
            };
        }

        var llmPages = pages.Where(p => p.Brand.Source == "llm").ToList();
        return new BrandSignals
        {
            PrimaryColor = llmPages.Select(p => p.Brand.PrimaryColor).FirstOrDefault(c => c != null),
            AccentColor = llmPages.Select(p => p.Brand.AccentColor).FirstOrDefault(c => c != null),
            BackgroundColor = llmPages.Select(p => p.Brand.BackgroundColor).FirstOrDefault(c => c != null),
            RawColorHints = llmPages.SelectMany(p => p.Brand.RawColorHints).Distinct().ToList(),
            Source = llmPages.Count > 0 ? "llm" : "none"
        };
    }
}
