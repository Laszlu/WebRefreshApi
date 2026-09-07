using ExCSS;
using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Requests;

namespace WebRefreshSiteBuilderAPI.Helpers;

public class CssColorExtractionHelper
{
    public static List<string> ExtractDeclaredColors(string css)
    {
        var parser = new StylesheetParser();
        var stylesheet = parser.Parse(css);

        var colors = new List<string>();

        foreach (var rule in stylesheet.StyleRules.Where(r => r.SelectorText == ":root"))
        {
            foreach (var prop in rule.Style)
            {
                if (prop.Name.StartsWith("--") && LooksLikeColor(prop.Value))
                    colors.Add(prop.Value);
            }
        }

        var frequency = new Dictionary<string, int>();
        foreach (var rule in stylesheet.StyleRules)
        {
            foreach (var prop in rule.Style.Where(p => p.Name is "color" or "background-color" && LooksLikeColor(p.Value)))
                frequency[prop.Value] = frequency.GetValueOrDefault(prop.Value) + 1;
        }

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