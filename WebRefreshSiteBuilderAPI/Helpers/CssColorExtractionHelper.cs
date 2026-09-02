using ExCSS;
using SiteBuilderContracts.Generation;

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

    private static bool LooksLikeColor(string value)
        => value.StartsWith('#') || value.StartsWith("rgb") || value.StartsWith("hsl");
    
    public static BrandSignals ResolveBrandColors(List<PageSpec> pages)
    {
        var cssPages = pages.Where(p => p.Brand.Source == "css").ToList();
        var relevant = cssPages.Count > 0 ? cssPages : pages;

        return new BrandSignals
        {
            PrimaryColor = relevant.Select(p => p.Brand.PrimaryColor).FirstOrDefault(c => c != null),
            AccentColor = relevant.Select(p => p.Brand.AccentColor).FirstOrDefault(c => c != null),
            BackgroundColor = relevant.Select(p => p.Brand.BackgroundColor).FirstOrDefault(c => c != null),
            RawColorHints = relevant.SelectMany(p => p.Brand.RawColorHints).Distinct().ToList(),
            Source = cssPages.Count > 0 ? "css" : (pages.Any(p => p.Brand.Source == "llm") ? "llm" : "none")
        };
    }
}